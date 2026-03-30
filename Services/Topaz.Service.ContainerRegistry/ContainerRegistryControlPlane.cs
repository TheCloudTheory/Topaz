using Azure.Core;
using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry;

internal sealed class ContainerRegistryControlPlane(
    ContainerRegistryResourceProvider provider,
    ResourceGroupControlPlane resourceGroupControlPlane,
    ITopazLogger logger) : IControlPlane
{
    private const string RegistryNotFoundCode = "ContainerRegistryNotFound";
    private const string RegistryNotFoundMessageTemplate = "Container registry '{0}' could not be found";

    private const string InvalidRegistryNameCode = "RegistryNameInvalid";
    private const string InvalidRegistryNameMessageTemplate =
        "The registry name '{0}' is invalid. A registry name must be between 5-50 alphanumeric characters.";

    public static ContainerRegistryControlPlane New(Pipeline eventPipeline, ITopazLogger logger) => new(
        new ContainerRegistryResourceProvider(logger),
        new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger),
            new SubscriptionControlPlane(eventPipeline, new SubscriptionResourceProvider(logger)), logger),
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
                new CreateOrUpdateContainerRegistryRequest
                {
                    Location = registry.Location,
                    Tags = registry.Tags,
                    Sku = new CreateOrUpdateContainerRegistryRequest.ContainerRegistrySku
                    {
                        Name = registry.Sku?.Name ?? "Basic"
                    },
                    Properties = new CreateOrUpdateContainerRegistryRequest.ContainerRegistryProperties
                    {
                        AdminUserEnabled = registry.Properties.AdminUserEnabled,
                        DataEndpointEnabled = registry.Properties.DataEndpointEnabled,
                        PublicNetworkAccess = registry.Properties.PublicNetworkAccess,
                        ZoneRedundancy = registry.Properties.ZoneRedundancy,
                        NetworkRuleBypassOptions = registry.Properties.NetworkRuleBypassOptions
                    }
                });

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
            var skuName = request.Sku?.Name ?? "Basic";
            var sku = new ResourceSku { Name = skuName };
            var properties = ContainerRegistryResourceProperties.FromRequest(registryName, request);
            resource = new ContainerRegistryResource(
                subscriptionIdentifier,
                resourceGroupIdentifier,
                registryName,
                new AzureLocation(request.Location ?? "eastus"),
                request.Tags,
                sku,
                properties);
        }
        else
        {
            var existingResource = existing.Resource!;
            var skuName = request.Sku?.Name ?? existingResource.Sku?.Name ?? "Basic";
            var sku = new ResourceSku { Name = skuName };
            ContainerRegistryResourceProperties.UpdateFromRequest(existingResource, request);
            resource = new ContainerRegistryResource(
                subscriptionIdentifier,
                resourceGroupIdentifier,
                registryName,
                new AzureLocation(request.Location ?? existingResource.Location ?? "eastus"),
                request.Tags ?? existingResource.Tags,
                sku,
                existingResource.Properties);
        }

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, registryName, resource, isCreate);

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

    private static bool IsNameValid(string name)
    {
        return name.Length is >= 5 and <= 50 && name.All(char.IsLetterOrDigit);
    }
}
