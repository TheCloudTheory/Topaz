using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.EventGrid.Models;
using Topaz.Service.EventGrid.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.EventGrid;

internal sealed class EventGridNamespaceControlPlane(Pipeline eventPipeline, ITopazLogger logger) : IControlPlane
{
    private static readonly string SharedAccessKeySubresource = nameof(Subresource.SharedAccessKeys).ToLowerInvariant();
    
    public static EventGridNamespaceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) => new(eventPipeline, logger);

    private readonly EventGridNamespaceResourceProvider _provider = new(logger);

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

    private readonly SubscriptionControlPlane _subscriptionControlPlane =
        SubscriptionControlPlane.New(eventPipeline, logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var @namespace = resource.As<EventGridNamespaceResource, EventGridNamespaceResourceProperties>();
        if (@namespace == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Event Grid instance.");
            return OperationResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(@namespace.Location))
        {
            logger.LogError($"Event Grid resource `{resource.Id}` is missing required location.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(@namespace.GetSubscription(), @namespace.GetResourceGroup(), @namespace.Name,
                @namespace);
            return result.Result is OperationResult.Created or OperationResult.Updated
                ? OperationResult.Success
                : OperationResult.Failed;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            return OperationResult.Failed;
        }
    }

    public ControlPlaneOperationResult<EventGridNamespaceResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string namespaceName,
        EventGridNamespaceResource request)
    {
        var resourceGroup = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroup.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<EventGridNamespaceResource>(
                OperationResult.NotFound, null, resourceGroup.Reason, resourceGroup.Code);
        }

        (bool IsValid, string? Error) validation;
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, namespaceName);

        if (existing.Resource != null)
        {
            existing.Resource.UpdateFromRequest(request);
            validation = existing.Resource.Validate<EventGridNamespaceResource>();
            if (!validation.IsValid)
            {
                return new ControlPlaneOperationResult<EventGridNamespaceResource>(OperationResult.BadRequest, null,
                    validation.Error, "BadRequest");
            }

            _provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, namespaceName, existing);
            return new ControlPlaneOperationResult<EventGridNamespaceResource>(OperationResult.Updated,
                existing.Resource);
        }

        var location = request.Location ?? resourceGroup.Resource!.Location!;
        var properties = EventGridNamespaceResourceProperties.FromRequest(request.Properties);
        var resource = new EventGridNamespaceResource(subscriptionIdentifier, resourceGroupIdentifier, namespaceName,
            location, request.Tags, request.Sku, properties);

        validation = resource.Validate<EventGridNamespaceResource>();
        if (!validation.IsValid)
        {
            return new ControlPlaneOperationResult<EventGridNamespaceResource>(OperationResult.BadRequest, null,
                validation.Error, "BadRequest");
        }

        _provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, namespaceName, resource,
            createOperation: true);
        
        // Also generate and create shared access keys
        var key1 = EventGridSharedAccessKey.Generate("key1");
        var key2 = EventGridSharedAccessKey.Generate("key2");
        
        _provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, key1.KeyName!, namespaceName,
            SharedAccessKeySubresource, key1);
        _provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, key2.KeyName!, namespaceName,
            SharedAccessKeySubresource, key2);

        return new ControlPlaneOperationResult<EventGridNamespaceResource>(OperationResult.Created, resource);
    }

    public ControlPlaneOperationResult<EventGridNamespaceResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string namespaceName)
    {
        var resource =
            _provider.GetAs<EventGridNamespaceResource>(subscriptionIdentifier, resourceGroupIdentifier, namespaceName);
        return resource == null
            ? new ControlPlaneOperationResult<EventGridNamespaceResource>(
                OperationResult.NotFound, null, "Event Grid namespace not found", "ResourceNotFound")
            : new ControlPlaneOperationResult<EventGridNamespaceResource>(OperationResult.Success, resource);
    }

    public ControlPlaneOperationResult Delete(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string namespaceName)
    {
        var resource = Get(subscriptionIdentifier, resourceGroupIdentifier, namespaceName);
        if (resource.Resource == null)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound, resource.Reason, resource.Code);
        }

        _provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, namespaceName, softDelete: false);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<EventGridNamespaceResource> Update(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string namespaceName, EventGridNamespaceResource request)
    {
        var resourceGroup = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroup.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<EventGridNamespaceResource>(
                OperationResult.NotFound, null, resourceGroup.Reason, resourceGroup.Code);
        }

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, namespaceName);
        if (existing.Resource == null)
        {
            return new ControlPlaneOperationResult<EventGridNamespaceResource>(OperationResult.NotFound, null,
                existing.Reason, existing.Code);
        }

        existing.Resource.UpdateFromRequest(request);

        _provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, namespaceName, existing.Resource,
            createOperation: true);

        return new ControlPlaneOperationResult<EventGridNamespaceResource>(OperationResult.Updated, existing.Resource);
    }

    public ControlPlaneOperationResult<EventGridNamespaceResource[]> ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string? topFilter)
    {
        var resourceGroup = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroup.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<EventGridNamespaceResource[]>(
                OperationResult.NotFound, null, resourceGroup.Reason, resourceGroup.Code);
        }

        var resources = _provider
            .ListAs<EventGridNamespaceResource>(subscriptionIdentifier, resourceGroupIdentifier, lookForNoOfSegments: 8)
            .Where(eg => eg.IsInResourceGroup(resourceGroupIdentifier) && eg.IsInSubscription(subscriptionIdentifier));

        if (!string.IsNullOrWhiteSpace(topFilter))
        {
            resources = resources.Take(int.Parse(topFilter));
        }

        return new ControlPlaneOperationResult<EventGridNamespaceResource[]>(OperationResult.Success, [.. resources]);
    }

    public ControlPlaneOperationResult<EventGridNamespaceResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier, string? topFilter)
    {
        var subscription = _subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscription.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<EventGridNamespaceResource[]>(
                OperationResult.NotFound, null, subscription.Reason, subscription.Code);
        }

        var resources = _provider
            .ListAs<EventGridNamespaceResource>(subscriptionIdentifier, null, lookForNoOfSegments: 8)
            .Where(eg => eg.IsInSubscription(subscriptionIdentifier));

        if (!string.IsNullOrWhiteSpace(topFilter))
        {
            resources = resources.Take(int.Parse(topFilter));
        }

        return new ControlPlaneOperationResult<EventGridNamespaceResource[]>(OperationResult.Success, [.. resources]);
    }

    public ControlPlaneOperationResult<EventGridSharedAccessKey[]> RegenerateKey(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string namespaceName, RegenerateNamespaceKeyRequest request)
    {
        var validation = request.Validate<RegenerateNamespaceKeyRequest>();
        if (!validation.IsValid)
        {
            return new ControlPlaneOperationResult<EventGridSharedAccessKey[]>(OperationResult.BadRequest, null, validation.Error,
                "BadRequest");
        }

        var resource = Get(subscriptionIdentifier, resourceGroupIdentifier, namespaceName);
        if (resource.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<EventGridSharedAccessKey[]>(OperationResult.NotFound, null,
                resource.Reason, resource.Code);
        }

        var key = EventGridSharedAccessKey.Generate(request.KeyName!);
        _provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, key.KeyName!, namespaceName,
            SharedAccessKeySubresource, key);

        var keys = _provider.ListSubresourcesAs<EventGridSharedAccessKey>(subscriptionIdentifier,
            resourceGroupIdentifier, namespaceName, SharedAccessKeySubresource);
        
        return new ControlPlaneOperationResult<EventGridSharedAccessKey[]>(OperationResult.Success, keys);
    }

    public ControlPlaneOperationResult<EventGridSharedAccessKey[]> ListKeys(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string name)
    {
        var resource = Get(subscriptionIdentifier, resourceGroupIdentifier, name);
        if (resource.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<EventGridSharedAccessKey[]>(OperationResult.NotFound, null,
                resource.Reason, resource.Code);
        }
        
        var keys = _provider.ListSubresourcesAs<EventGridSharedAccessKey>(subscriptionIdentifier,
            resourceGroupIdentifier, name, SharedAccessKeySubresource);
        
        return new ControlPlaneOperationResult<EventGridSharedAccessKey[]>(OperationResult.Success, keys);
    }
}