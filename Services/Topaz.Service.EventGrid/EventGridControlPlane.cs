using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.EventGrid.Models;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.EventGrid;

internal sealed class EventGridControlPlane(Pipeline eventPipeline, ITopazLogger logger) : IControlPlane
{
    public static EventGridControlPlane New(Pipeline eventPipeline, ITopazLogger logger) => new(eventPipeline, logger); 
    
    private readonly EventGridResourceProvider _provider = new(logger);
    
    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);
    
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
            var result = CreateOrUpdate(@namespace.GetSubscription(), @namespace.GetResourceGroup(), @namespace.Name, @namespace);
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
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string namespaceName,
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
            return new ControlPlaneOperationResult<EventGridNamespaceResource>(OperationResult.Updated, existing.Resource);
        }

        var location = request.Location ?? resourceGroup.Resource!.Location!;
        var properties = EventGridNamespaceResourceProperties.FromRequest(request.Properties);
        var resource = new EventGridNamespaceResource(subscriptionIdentifier, resourceGroupIdentifier, namespaceName, location, request.Tags, request.Sku, properties);

        validation = resource.Validate<EventGridNamespaceResource>();
        if (!validation.IsValid)
        {
            return new ControlPlaneOperationResult<EventGridNamespaceResource>(OperationResult.BadRequest, null,
                validation.Error, "BadRequest");
        }
        
        _provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, namespaceName, resource, createOperation: true);
        
        return new ControlPlaneOperationResult<EventGridNamespaceResource>(OperationResult.Created, resource);
    }

    public ControlPlaneOperationResult<EventGridNamespaceResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string namespaceName)
    {
        var resource = _provider.GetAs<EventGridNamespaceResource>(subscriptionIdentifier, resourceGroupIdentifier, namespaceName);
        return resource == null
            ? new ControlPlaneOperationResult<EventGridNamespaceResource>(
                OperationResult.NotFound, null, "Event Grid namespace not found", "ResourceNotFound")
            : new ControlPlaneOperationResult<EventGridNamespaceResource>(OperationResult.Success, resource);
    }
}