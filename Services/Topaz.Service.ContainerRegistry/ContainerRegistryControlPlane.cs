using Azure.Core;
using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.ManagedIdentity;
using Topaz.Service.ContainerRegistry.Models.Responses;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry;

internal sealed class ContainerRegistryControlPlane(
    ContainerRegistryResourceProvider provider,
    ResourceGroupControlPlane resourceGroupControlPlane,
    SystemAssignedIdentityControlPlane systemAssignedIdentityControlPlane,
    ITopazLogger logger) : IControlPlane
{
    private const string RegistryNotFoundCode = "ContainerRegistryNotFound";
    private const string RegistryNotFoundMessageTemplate = "Container registry '{0}' could not be found";

    private const string InvalidRegistryNameCode = "RegistryNameInvalid";
    private const string InvalidRegistryNameMessageTemplate =
        "The registry name '{0}' is invalid. A registry name must be between 5-50 alphanumeric characters.";

    private const string MissingLocationCode = "LocationRequired";
    private const string MissingLocationMessage = "The 'location' property is required when creating a container registry.";

    public static ContainerRegistryControlPlane New(Pipeline eventPipeline, ITopazLogger logger) => new(
        new ContainerRegistryResourceProvider(logger),
        new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger),
            SubscriptionControlPlane.New(eventPipeline, logger), logger),
        SystemAssignedIdentityControlPlane.New(logger),
        logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var registry = resource.As<ContainerRegistryResource, ContainerRegistryResourceProperties>();
        if (registry == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Container Registry instance.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(registry.GetSubscription(), registry.GetResourceGroup(), registry.Name,
                CreateOrUpdateContainerRegistryRequest.FromResource(registry));

            return result.Result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            return OperationResult.Failed;
        }
    }

    public ControlPlaneOperationResult<ContainerRegistryResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        CreateOrUpdateContainerRegistryRequest request)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(CreateOrUpdate),
            "Executing {0}: registry={1}, resourceGroup={2}, subscription={3}",
            nameof(CreateOrUpdate), registryName, resourceGroupIdentifier.Value, subscriptionIdentifier.Value);

        if (!IsNameValid(registryName))
        {
            logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(CreateOrUpdate),
                "Executing {0}: Registry name '{1}' is invalid.", nameof(CreateOrUpdate), registryName);
            return new ControlPlaneOperationResult<ContainerRegistryResource>(
                OperationResult.Failed, null,
                string.Format(InvalidRegistryNameMessageTemplate, registryName),
                InvalidRegistryNameCode);
        }

        var resourceGroupOperation = resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(CreateOrUpdate),
                "Executing {0}: Resource group '{1}' not found.", nameof(CreateOrUpdate), resourceGroupIdentifier.Value);
            return new ControlPlaneOperationResult<ContainerRegistryResource>(
                OperationResult.Failed, null,
                resourceGroupOperation.Reason,
                resourceGroupOperation.Code);
        }

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        var isCreate = existing.Result == OperationResult.NotFound;
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(CreateOrUpdate),
            "Executing {0}: Operation is {1}.", nameof(CreateOrUpdate), isCreate ? "create" : "update");

        ContainerRegistryResource resource;
        if (isCreate)
        {
            if (string.IsNullOrWhiteSpace(request.Location))
            {
                return new ControlPlaneOperationResult<ContainerRegistryResource>(
                    OperationResult.Failed, null,
                    MissingLocationMessage,
                    MissingLocationCode);
            }

            var skuName = request.Sku?.Name ?? "Basic";
            var sku = new ResourceSku { Name = skuName };
            var properties = ContainerRegistryResourceProperties.FromRequest(registryName, request);
            resource = new ContainerRegistryResource(
                subscriptionIdentifier,
                resourceGroupIdentifier,
                registryName,
                new AzureLocation(request.Location),
                request.Tags,
                sku,
                properties);
        }
        else
        {
            var existingResource = existing.Resource!;
            var resolvedLocation = request.Location ?? existingResource.Location;
            if (string.IsNullOrWhiteSpace(resolvedLocation))
            {
                return new ControlPlaneOperationResult<ContainerRegistryResource>(
                    OperationResult.Failed, null,
                    MissingLocationMessage,
                    MissingLocationCode);
            }

            var skuName = request.Sku?.Name ?? existingResource.Sku?.Name ?? "Basic";
            var sku = new ResourceSku { Name = skuName };
            ContainerRegistryResourceProperties.UpdateFromRequest(existingResource, request);
            resource = new ContainerRegistryResource(
                subscriptionIdentifier,
                resourceGroupIdentifier,
                registryName,
                new AzureLocation(resolvedLocation),
                request.Tags ?? existingResource.Tags,
                sku,
                existingResource.Properties);
        }

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, registryName, resource, isCreate);

        if (!string.Equals(request.Identity?.Type, "SystemAssigned", StringComparison.OrdinalIgnoreCase))
            return new ControlPlaneOperationResult<ContainerRegistryResource>(
                isCreate ? OperationResult.Created : OperationResult.Updated,
                resource, null, null);
        
        var identityOperation = systemAssignedIdentityControlPlane.CreateOrUpdate(resource.Id);
        if (identityOperation.Resource == null)
            return new ControlPlaneOperationResult<ContainerRegistryResource>(
                isCreate ? OperationResult.Created : OperationResult.Updated,
                resource, null, null);
            
        resource.Identity = new ResourceIdentity
        {
            Type = "SystemAssigned",
            PrincipalId = identityOperation.Resource.Properties.PrincipalId,
            TenantId = identityOperation.Resource.Properties.TenantId
        };
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, registryName, resource, false);

        return new ControlPlaneOperationResult<ContainerRegistryResource>(
            isCreate ? OperationResult.Created : OperationResult.Updated,
            resource, null, null);
    }

    public ControlPlaneOperationResult<ContainerRegistryResource> Get(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(Get),
            "Executing {0}: registry={1}, resourceGroup={2}, subscription={3}",
            nameof(Get), registryName, resourceGroupIdentifier.Value, subscriptionIdentifier.Value);

        var resource = provider.GetAs<ContainerRegistryResource>(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (resource == null)
        {
            logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(Get),
                "Executing {0}: Registry '{1}' not found.", nameof(Get), registryName);
            return new ControlPlaneOperationResult<ContainerRegistryResource>(
                OperationResult.NotFound, null,
                string.Format(RegistryNotFoundMessageTemplate, registryName),
                RegistryNotFoundCode);
        }

        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(Get),
            "Executing {0}: Registry '{1}' found.", nameof(Get), registryName);
        return new ControlPlaneOperationResult<ContainerRegistryResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<ContainerRegistryResource> Delete(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(Delete),
            "Executing {0}: registry={1}, resourceGroup={2}, subscription={3}",
            nameof(Delete), registryName, resourceGroupIdentifier.Value, subscriptionIdentifier.Value);

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (existing.Result == OperationResult.NotFound)
        {
            logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(Delete),
                "Executing {0}: Registry '{1}' not found.", nameof(Delete), registryName);
            return new ControlPlaneOperationResult<ContainerRegistryResource>(
                OperationResult.NotFound, null,
                string.Format(RegistryNotFoundMessageTemplate, registryName),
                RegistryNotFoundCode);
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(Delete),
            "Executing {0}: Registry '{1}' deleted.", nameof(Delete), registryName);

        return new ControlPlaneOperationResult<ContainerRegistryResource>(
            OperationResult.Deleted, existing.Resource, null, null);
    }

    // LocalDirectoryPath has 5 segments; add 3 for .topaz prefix, registry-name dir, and metadata.json
    private static readonly uint RegistryFileSegmentCount =
        (uint)(ContainerRegistryService.LocalDirectoryPath.Split("/").Length + 3);

    public ControlPlaneOperationResult<ContainerRegistryResource[]> ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ListByResourceGroup),
            "Executing {0}: resourceGroup={1}, subscription={2}",
            nameof(ListByResourceGroup), resourceGroupIdentifier.Value, subscriptionIdentifier.Value);

        var resources = provider.ListAs<ContainerRegistryResource>(subscriptionIdentifier, resourceGroupIdentifier,
                lookForNoOfSegments: RegistryFileSegmentCount)
            .Where(r => r.IsInResourceGroup(resourceGroupIdentifier))
            .ToArray();

        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ListByResourceGroup),
            "Executing {0}: Found {1} registries.", nameof(ListByResourceGroup), resources.Length);
        return new ControlPlaneOperationResult<ContainerRegistryResource[]>(
            OperationResult.Success, resources, null, null);
    }

    public ControlPlaneOperationResult<ContainerRegistryResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ListBySubscription),
            "Executing {0}: subscription={1}", nameof(ListBySubscription), subscriptionIdentifier.Value);

        var resources = provider.ListAs<ContainerRegistryResource>(subscriptionIdentifier, null,
                lookForNoOfSegments: RegistryFileSegmentCount)
            .Where(r => r.IsInSubscription(subscriptionIdentifier))
            .ToArray();

        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ListBySubscription),
            "Executing {0}: Found {1} registries.", nameof(ListBySubscription), resources.Length);
        return new ControlPlaneOperationResult<ContainerRegistryResource[]>(
            OperationResult.Success, resources, null, null);
    }

    public bool IsNameAvailable(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier? resourceGroupIdentifier,
        string registryName)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(IsNameAvailable),
            "Executing {0}: registry={1}, subscription={2}",
            nameof(IsNameAvailable), registryName, subscriptionIdentifier.Value);

        if (!IsNameValid(registryName)) return false;

        if (resourceGroupIdentifier != null)
        {
            var resource = provider.GetAs<ContainerRegistryResource>(subscriptionIdentifier, resourceGroupIdentifier, registryName);
            return resource == null;
        }

        // search across all resource groups in the subscription
        var allRegistries = provider.ListAs<ContainerRegistryResource>(subscriptionIdentifier, null,
            lookForNoOfSegments: RegistryFileSegmentCount);
        var isAvailable = allRegistries.All(r => !string.Equals(r.Name, registryName, StringComparison.OrdinalIgnoreCase));
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(IsNameAvailable),
            "Executing {0}: Registry '{1}' is {2}.", nameof(IsNameAvailable), registryName, isAvailable ? "available" : "unavailable");
        return isAvailable;
    }

    /// <summary>
    /// Regenerates one of the admin passwords for the specified container registry.
    /// </summary>
    /// <param name="subscriptionIdentifier">The subscription that owns the registry.</param>
    /// <param name="resourceGroupIdentifier">The resource group that owns the registry.</param>
    /// <param name="registryName">The registry name.</param>
    /// <param name="passwordName">The password to regenerate: "password" or "password2".</param>
    /// <returns>
    /// The updated registry resource on success, or a failure result if the registry is not found
    /// or admin user is disabled.
    /// </returns>
    public ControlPlaneOperationResult<ContainerRegistryResource> RegenerateCredential(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string passwordName)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(RegenerateCredential),
            "Executing {0}: registry={1}, password={2}",
            nameof(RegenerateCredential), registryName, passwordName);

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (existing.Result == OperationResult.NotFound)
            return existing;

        var resource = existing.Resource!;

        if (!resource.Properties.AdminUserEnabled)
        {
            logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(RegenerateCredential),
                "Executing {0}: Admin user is disabled for registry '{1}'.", nameof(RegenerateCredential), registryName);
            return new ControlPlaneOperationResult<ContainerRegistryResource>(
                OperationResult.Failed, null,
                $"Admin user is disabled for registry '{registryName}'.",
                "ADMIN_USER_DISABLED");
        }

        var newPassword = ContainerRegistryResourceProperties.GenerateAdminPassword();

        if (string.Equals(passwordName, "password2", StringComparison.OrdinalIgnoreCase))
            resource.Properties.AdminPassword2 = newPassword;
        else
            resource.Properties.AdminPassword = newPassword;

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, registryName, resource, false);

        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(RegenerateCredential),
            "Executing {0}: Password '{1}' regenerated for registry '{2}'.",
            nameof(RegenerateCredential), passwordName, registryName);

        return new ControlPlaneOperationResult<ContainerRegistryResource>(OperationResult.Updated, resource, null, null);
    }

    private static bool IsNameValid(string name)
    {
        return name.Length is >= 5 and <= 50 && name.All(char.IsLetterOrDigit);
    }

    /// <summary>
    /// Returns quota usages for the specified container registry derived from its SKU tier.
    /// </summary>
    /// <param name="subscriptionIdentifier">The subscription that owns the registry.</param>
    /// <param name="resourceGroupIdentifier">The resource group that owns the registry.</param>
    /// <param name="registryName">The registry name.</param>
    /// <returns>
    /// The usage entries on success, or a not-found result if the registry does not exist.
    /// </returns>
    public ControlPlaneOperationResult<RegistryUsage[]> ListUsages(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ListUsages),
            "Executing {0}: registry={1}, resourceGroup={2}, subscription={3}",
            nameof(ListUsages), registryName, resourceGroupIdentifier.Value, subscriptionIdentifier.Value);

        var result = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (result.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<RegistryUsage[]>(OperationResult.NotFound, null, result.Reason, result.Code);

        var usages = ContainerRegistryResourceProperties.GetUsagesForSku(result.Resource!.Sku?.Name);
        return new ControlPlaneOperationResult<RegistryUsage[]>(OperationResult.Success, usages, null, null);
    }

    private const string TasksSubresource = "tasks";
    private const string RunsSubresource = "runs";

    private const string TaskNotFoundCode = "TaskNotFound";
    private const string TaskNotFoundMessageTemplate = "ACR task '{0}' could not be found in registry '{1}'";

    private const string RunNotFoundCode = "RunNotFound";
    private const string RunNotFoundMessageTemplate = "ACR run '{0}' could not be found in registry '{1}'";

    public ControlPlaneOperationResult<AcrTaskResource> CreateOrUpdateTask(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string taskName,
        CreateOrUpdateAcrTaskRequest request)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(CreateOrUpdateTask),
            "Executing {0}: registry={1}, task={2}", nameof(CreateOrUpdateTask), registryName, taskName);

        var registryOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (registryOperation.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<AcrTaskResource>(
                OperationResult.NotFound, null, registryOperation.Reason, registryOperation.Code);

        var existing = provider.GetSubresourceAs<AcrTaskResource>(
            subscriptionIdentifier, resourceGroupIdentifier, taskName, registryName, TasksSubresource);

        AcrTaskResource resource;
        if (existing == null)
        {
            var location = request.Location ?? registryOperation.Resource!.Location ?? "eastus";
            var properties = AcrTaskResourceProperties.FromRequest(request);
            resource = new AcrTaskResource(
                subscriptionIdentifier, resourceGroupIdentifier, registryName, taskName,
                location, request.Tags, request.Identity, properties);
        }
        else
        {
            AcrTaskResourceProperties.UpdateFromRequest(existing, new UpdateAcrTaskRequest
            {
                Tags = request.Tags ?? existing.Tags,
                Identity = request.Identity ?? existing.Identity,
                Properties = request.Properties == null ? null : new UpdateAcrTaskRequest.UpdateAcrTaskRequestProperties
                {
                    Status = request.Properties.Status,
                    Timeout = request.Properties.Timeout,
                    Platform = request.Properties.Platform,
                    AgentConfiguration = request.Properties.AgentConfiguration,
                    Step = request.Properties.Step,
                    Trigger = request.Properties.Trigger,
                    Credentials = request.Properties.Credentials
                }
            });
            resource = existing;
        }

        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, taskName, registryName, TasksSubresource, resource);

        var result = existing == null ? OperationResult.Created : OperationResult.Updated;
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(CreateOrUpdateTask),
            "Executing {0}: task '{1}' {2}.", nameof(CreateOrUpdateTask), taskName,
            existing == null ? "created" : "updated");
        return new ControlPlaneOperationResult<AcrTaskResource>(result, resource, null, null);
    }

    public ControlPlaneOperationResult<AcrTaskResource> GetTask(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string taskName)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(GetTask),
            "Executing {0}: registry={1}, task={2}", nameof(GetTask), registryName, taskName);

        var registryOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (registryOperation.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<AcrTaskResource>(
                OperationResult.NotFound, null, registryOperation.Reason, registryOperation.Code);

        var resource = provider.GetSubresourceAs<AcrTaskResource>(
            subscriptionIdentifier, resourceGroupIdentifier, taskName, registryName, TasksSubresource);

        return resource == null
            ? new ControlPlaneOperationResult<AcrTaskResource>(
                OperationResult.NotFound, null,
                string.Format(TaskNotFoundMessageTemplate, taskName, registryName),
                TaskNotFoundCode)
            : new ControlPlaneOperationResult<AcrTaskResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult DeleteTask(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string taskName)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(DeleteTask),
            "Executing {0}: registry={1}, task={2}", nameof(DeleteTask), registryName, taskName);

        var existing = provider.GetSubresourceAs<AcrTaskResource>(
            subscriptionIdentifier, resourceGroupIdentifier, taskName, registryName, TasksSubresource);

        if (existing == null)
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                string.Format(TaskNotFoundMessageTemplate, taskName, registryName),
                TaskNotFoundCode);

        provider.DeleteSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, taskName, registryName, TasksSubresource);

        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(DeleteTask),
            "Executing {0}: task '{1}' deleted.", nameof(DeleteTask), taskName);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<AcrTaskResource[]> ListTasks(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ListTasks),
            "Executing {0}: registry={1}", nameof(ListTasks), registryName);

        var registryOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (registryOperation.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<AcrTaskResource[]>(
                OperationResult.NotFound, null, registryOperation.Reason, registryOperation.Code);

        var tasks = provider.ListSubresourcesAs<AcrTaskResource>(
            subscriptionIdentifier, resourceGroupIdentifier, registryName, TasksSubresource);

        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ListTasks),
            "Executing {0}: Found {1} tasks.", nameof(ListTasks), tasks.Length);
        return new ControlPlaneOperationResult<AcrTaskResource[]>(OperationResult.Success, tasks, null, null);
    }

    public ControlPlaneOperationResult<AcrTaskResource> UpdateTask(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string taskName,
        UpdateAcrTaskRequest request)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(UpdateTask),
            "Executing {0}: registry={1}, task={2}", nameof(UpdateTask), registryName, taskName);

        var existing = provider.GetSubresourceAs<AcrTaskResource>(
            subscriptionIdentifier, resourceGroupIdentifier, taskName, registryName, TasksSubresource);

        if (existing == null)
            return new ControlPlaneOperationResult<AcrTaskResource>(
                OperationResult.NotFound, null,
                string.Format(TaskNotFoundMessageTemplate, taskName, registryName),
                TaskNotFoundCode);

        AcrTaskResourceProperties.UpdateFromRequest(existing, request);
        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, taskName, registryName, TasksSubresource, existing);

        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(UpdateTask),
            "Executing {0}: task '{1}' updated.", nameof(UpdateTask), taskName);
        return new ControlPlaneOperationResult<AcrTaskResource>(OperationResult.Updated, existing, null, null);
    }

    public ControlPlaneOperationResult<AcrRunResource> TriggerTaskRun(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string taskName,
        RunAcrTaskRequest request)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(TriggerTaskRun),
            "Executing {0}: registry={1}, task={2}", nameof(TriggerTaskRun), registryName, taskName);

        var registryOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (registryOperation.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<AcrRunResource>(
                OperationResult.NotFound, null, registryOperation.Reason, registryOperation.Code);

        var taskResource = provider.GetSubresourceAs<AcrTaskResource>(
            subscriptionIdentifier, resourceGroupIdentifier, taskName, registryName, TasksSubresource);
        if (taskResource == null)
            return new ControlPlaneOperationResult<AcrRunResource>(
                OperationResult.NotFound, null,
                string.Format(TaskNotFoundMessageTemplate, taskName, registryName),
                TaskNotFoundCode);

        var runId = Guid.NewGuid().ToString("N")[..8];
        var properties = AcrRunResourceProperties.FromTaskRun(taskName, runId, request);
        var resource = new AcrRunResource(subscriptionIdentifier, resourceGroupIdentifier, registryName, runId, properties);

        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource, resource);

        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(TriggerTaskRun),
            "Executing {0}: run '{1}' created for task '{2}'.", nameof(TriggerTaskRun), runId, taskName);
        return new ControlPlaneOperationResult<AcrRunResource>(OperationResult.Created, resource, null, null);
    }

    public ControlPlaneOperationResult<AcrRunResource> ScheduleRun(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        ScheduleAcrRunRequest request)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ScheduleRun),
            "Executing {0}: registry={1}", nameof(ScheduleRun), registryName);

        var registryOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (registryOperation.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<AcrRunResource>(
                OperationResult.NotFound, null, registryOperation.Reason, registryOperation.Code);

        var runId = Guid.NewGuid().ToString("N")[..8];
        var properties = AcrRunResourceProperties.FromScheduleRun(runId, request);
        var resource = new AcrRunResource(subscriptionIdentifier, resourceGroupIdentifier, registryName, runId, properties);

        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource, resource);

        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ScheduleRun),
            "Executing {0}: run '{1}' created.", nameof(ScheduleRun), runId);
        return new ControlPlaneOperationResult<AcrRunResource>(OperationResult.Created, resource, null, null);
    }

    public ControlPlaneOperationResult<AcrRunResource> GetRun(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string runId)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(GetRun),
            "Executing {0}: registry={1}, run={2}", nameof(GetRun), registryName, runId);

        var registryOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (registryOperation.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<AcrRunResource>(
                OperationResult.NotFound, null, registryOperation.Reason, registryOperation.Code);

        var resource = provider.GetSubresourceAs<AcrRunResource>(
            subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource);

        return resource == null
            ? new ControlPlaneOperationResult<AcrRunResource>(
                OperationResult.NotFound, null,
                string.Format(RunNotFoundMessageTemplate, runId, registryName),
                RunNotFoundCode)
            : new ControlPlaneOperationResult<AcrRunResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<AcrRunResource[]> ListRuns(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ListRuns),
            "Executing {0}: registry={1}", nameof(ListRuns), registryName);

        var registryOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (registryOperation.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<AcrRunResource[]>(
                OperationResult.NotFound, null, registryOperation.Reason, registryOperation.Code);

        var runs = provider.ListSubresourcesAs<AcrRunResource>(
            subscriptionIdentifier, resourceGroupIdentifier, registryName, RunsSubresource);

        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ListRuns),
            "Executing {0}: Found {1} runs.", nameof(ListRuns), runs.Length);
        return new ControlPlaneOperationResult<AcrRunResource[]>(OperationResult.Success, runs, null, null);
    }

    public ControlPlaneOperationResult<AcrRunResource> UpdateRun(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string runId,
        UpdateAcrRunRequest request)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(UpdateRun),
            "Executing {0}: registry={1}, run={2}", nameof(UpdateRun), registryName, runId);

        var existing = provider.GetSubresourceAs<AcrRunResource>(
            subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource);

        if (existing == null)
            return new ControlPlaneOperationResult<AcrRunResource>(
                OperationResult.NotFound, null,
                string.Format(RunNotFoundMessageTemplate, runId, registryName),
                RunNotFoundCode);

        AcrRunResourceProperties.UpdateFromRequest(existing, request);
        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource, existing);

        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(UpdateRun),
            "Executing {0}: run '{1}' updated.", nameof(UpdateRun), runId);
        return new ControlPlaneOperationResult<AcrRunResource>(OperationResult.Updated, existing, null, null);
    }
}
