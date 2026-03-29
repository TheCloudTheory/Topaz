using Azure.Core;
using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
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
        throw new NotImplementedException();
    }

    public ControlPlaneOperationResult<ContainerRegistryResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        CreateOrUpdateContainerRegistryRequest request)
    {
        if (!IsNameValid(registryName))
        {
            return new ControlPlaneOperationResult<ContainerRegistryResource>(
                OperationResult.Failed, null,
                string.Format(InvalidRegistryNameMessageTemplate, registryName),
                InvalidRegistryNameCode);
        }

        var resourceGroupOperation = resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ContainerRegistryResource>(
                OperationResult.Failed, null,
                resourceGroupOperation.Reason,
                resourceGroupOperation.Code);
        }

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        var isCreate = existing.Result == OperationResult.NotFound;

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
        var resource = provider.GetAs<ContainerRegistryResource>(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (resource == null)
        {
            return new ControlPlaneOperationResult<ContainerRegistryResource>(
                OperationResult.NotFound, null,
                string.Format(RegistryNotFoundMessageTemplate, registryName),
                RegistryNotFoundCode);
        }

        return new ControlPlaneOperationResult<ContainerRegistryResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<ContainerRegistryResource> Delete(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName)
    {
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ContainerRegistryResource>(
                OperationResult.NotFound, null,
                string.Format(RegistryNotFoundMessageTemplate, registryName),
                RegistryNotFoundCode);
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, registryName);

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
        var resources = provider.ListAs<ContainerRegistryResource>(subscriptionIdentifier, resourceGroupIdentifier,
                lookForNoOfSegments: RegistryFileSegmentCount)
            .Where(r => r.IsInResourceGroup(resourceGroupIdentifier))
            .ToArray();

        return new ControlPlaneOperationResult<ContainerRegistryResource[]>(
            OperationResult.Success, resources, null, null);
    }

    public ControlPlaneOperationResult<ContainerRegistryResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<ContainerRegistryResource>(subscriptionIdentifier, null,
                lookForNoOfSegments: RegistryFileSegmentCount)
            .Where(r => r.IsInSubscription(subscriptionIdentifier))
            .ToArray();

        return new ControlPlaneOperationResult<ContainerRegistryResource[]>(
            OperationResult.Success, resources, null, null);
    }

    public bool IsNameAvailable(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier? resourceGroupIdentifier,
        string registryName)
    {
        if (!IsNameValid(registryName)) return false;

        if (resourceGroupIdentifier != null)
        {
            var resource = provider.GetAs<ContainerRegistryResource>(subscriptionIdentifier, resourceGroupIdentifier, registryName);
            return resource == null;
        }

        // search across all resource groups in the subscription
        var allRegistries = provider.ListAs<ContainerRegistryResource>(subscriptionIdentifier, null);
        return allRegistries.All(r => !string.Equals(r.Name, registryName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsNameValid(string name)
    {
        return name.Length is >= 5 and <= 50 && name.All(char.IsLetterOrDigit);
    }
}
