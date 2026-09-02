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

internal sealed class EventGridTopicControlPlane(Pipeline eventPipeline, ITopazLogger logger) : IControlPlane
{
    private static readonly string SharedAccessKeySubresource = nameof(Subresource.SharedAccessKeys).ToLowerInvariant();
    private static readonly string EventSubscriptionSubresource = nameof(Subresource.TopicEventSubscriptions).ToLowerInvariant();
    
    public static EventGridTopicControlPlane New(Pipeline eventPipeline, ITopazLogger logger) => new(eventPipeline, logger);

    private readonly EventGridTopicResourceProvider _provider = new(logger);

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

    private readonly SubscriptionControlPlane _subscriptionControlPlane =
        SubscriptionControlPlane.New(eventPipeline, logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var topic = resource.As<EventGridTopicResource, EventGridTopicResourceProperties>();
        if (topic == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Event Grid topic instance.");
            return OperationResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(topic.Location))
        {
            logger.LogError($"Event Grid resource `{resource.Id}` is missing required location.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(topic.GetSubscription(), topic.GetResourceGroup(), topic.Name,
                topic);
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

    public ControlPlaneOperationResult<EventGridTopicResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string topicName, EventGridTopicResource request)
    {
        var resourceGroup = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroup.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<EventGridTopicResource>(
                OperationResult.NotFound, null, resourceGroup.Reason, resourceGroup.Code);
        }

        (bool IsValid, string? Error) validation;
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, topicName);

        if (existing.Resource != null)
        {
            existing.Resource.UpdateFromRequest(request);
            validation = existing.Resource.Validate<EventGridTopicResource>();
            if (!validation.IsValid)
            {
                return new ControlPlaneOperationResult<EventGridTopicResource>(OperationResult.BadRequest, null,
                    validation.Error, "BadRequest");
            }

            _provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, topicName, existing);
            return new ControlPlaneOperationResult<EventGridTopicResource>(OperationResult.Updated,
                existing.Resource);
        }

        var location = request.Location ?? resourceGroup.Resource!.Location!;
        var properties = EventGridTopicResourceProperties.FromRequest(request.Properties);
        var resource = new EventGridTopicResource(subscriptionIdentifier, resourceGroupIdentifier, topicName,
            location, request.Tags, properties);

        validation = resource.Validate<EventGridTopicResource>();
        if (!validation.IsValid)
        {
            return new ControlPlaneOperationResult<EventGridTopicResource>(OperationResult.BadRequest, null,
                validation.Error, "BadRequest");
        }

        _provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, topicName, resource,
            createOperation: true);
        
        // Also generate and create shared access keys
        var key1 = EventGridSharedAccessKey.Generate("key1");
        var key2 = EventGridSharedAccessKey.Generate("key2");
        
        _provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, key1.KeyName!, topicName,
            SharedAccessKeySubresource, key1);
        _provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, key2.KeyName!, topicName,
            SharedAccessKeySubresource, key2);

        return new ControlPlaneOperationResult<EventGridTopicResource>(OperationResult.Created, resource);
    }
    
    public ControlPlaneOperationResult<EventGridTopicResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string topicName)
    {
        var resource =
            _provider.GetAs<EventGridTopicResource>(subscriptionIdentifier, resourceGroupIdentifier, topicName);
        return resource == null
            ? new ControlPlaneOperationResult<EventGridTopicResource>(
                OperationResult.NotFound, null, "Event Grid topic not found", "ResourceNotFound")
            : new ControlPlaneOperationResult<EventGridTopicResource>(OperationResult.Success, resource);
    }
    
    public ControlPlaneOperationResult Delete(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string topicName)
    {
        var resource = Get(subscriptionIdentifier, resourceGroupIdentifier, topicName);
        if (resource.Resource == null)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound, resource.Reason, resource.Code);
        }

        _provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, topicName, softDelete: false);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }
    
    public ControlPlaneOperationResult<EventGridTopicResource> Update(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string topicName, EventGridTopicResource request)
    {
        var resourceGroup = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroup.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<EventGridTopicResource>(
                OperationResult.NotFound, null, resourceGroup.Reason, resourceGroup.Code);
        }

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, topicName);
        if (existing.Resource == null)
        {
            return new ControlPlaneOperationResult<EventGridTopicResource>(OperationResult.NotFound, null,
                existing.Reason, existing.Code);
        }

        existing.Resource.UpdateFromRequest(request);

        _provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, topicName, existing.Resource,
            createOperation: true);

        return new ControlPlaneOperationResult<EventGridTopicResource>(OperationResult.Updated, existing.Resource);
    }
    
    public ControlPlaneOperationResult<EventGridTopicResource[]> ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string? topFilter)
    {
        var resourceGroup = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroup.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<EventGridTopicResource[]>(
                OperationResult.NotFound, null, resourceGroup.Reason, resourceGroup.Code);
        }

        var resources = _provider
            .ListAs<EventGridTopicResource>(subscriptionIdentifier, resourceGroupIdentifier, lookForNoOfSegments: 8)
            .Where(eg => eg.IsInResourceGroup(resourceGroupIdentifier) && eg.IsInSubscription(subscriptionIdentifier));

        if (!string.IsNullOrWhiteSpace(topFilter))
        {
            resources = resources.Take(int.Parse(topFilter));
        }

        return new ControlPlaneOperationResult<EventGridTopicResource[]>(OperationResult.Success, [.. resources]);
    }

    public ControlPlaneOperationResult<EventGridTopicResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier, string? topFilter)
    {
        var subscription = _subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscription.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<EventGridTopicResource[]>(
                OperationResult.NotFound, null, subscription.Reason, subscription.Code);
        }

        var resources = _provider
            .ListAs<EventGridTopicResource>(subscriptionIdentifier, null, lookForNoOfSegments: 8)
            .Where(eg => eg.IsInSubscription(subscriptionIdentifier));

        if (!string.IsNullOrWhiteSpace(topFilter))
        {
            resources = resources.Take(int.Parse(topFilter));
        }

        return new ControlPlaneOperationResult<EventGridTopicResource[]>(OperationResult.Success, [.. resources]);
    }

    public ControlPlaneOperationResult<EventGridSharedAccessKey[]> RegenerateKey(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string topicName, RegenerateNamespaceKeyRequest request)
    {
        var validation = request.Validate<RegenerateNamespaceKeyRequest>();
        if (!validation.IsValid)
        {
            return new ControlPlaneOperationResult<EventGridSharedAccessKey[]>(OperationResult.BadRequest, null, validation.Error,
                "BadRequest");
        }

        var resource = Get(subscriptionIdentifier, resourceGroupIdentifier, topicName);
        if (resource.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<EventGridSharedAccessKey[]>(OperationResult.NotFound, null,
                resource.Reason, resource.Code);
        }

        var key = EventGridSharedAccessKey.Generate(request.KeyName!);
        _provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, key.KeyName!, topicName,
            SharedAccessKeySubresource, key);

        var keys = _provider.ListSubresourcesAs<EventGridSharedAccessKey>(subscriptionIdentifier,
            resourceGroupIdentifier, topicName, SharedAccessKeySubresource);
        
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

    public ControlPlaneOperationResult<EventSubscriptionSubresource> CreateOrUpdateEventSubscription(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string topicName, string eventSubscriptionName, EventSubscriptionSubresourceProperties request)
    {
        var topicResource = Get(subscriptionIdentifier, resourceGroupIdentifier, topicName);
        if (topicResource.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<EventSubscriptionSubresource>(OperationResult.NotFound, null,
                topicResource.Reason, topicResource.Code);
        }
        
        var eventSubscription = GetEventSubscription(subscriptionIdentifier, resourceGroupIdentifier, topicName,
            eventSubscriptionName);
        if (eventSubscription.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<EventSubscriptionSubresource>(OperationResult.NotFound, null,
                eventSubscription.Reason, eventSubscription.Code);
        }

        if (eventSubscription.Resource != null)
        {
            eventSubscription.Resource.UpdateFromRequest(request);

            _provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, eventSubscriptionName,
                topicName, EventSubscriptionSubresource, eventSubscription.Resource);
            return new ControlPlaneOperationResult<EventSubscriptionSubresource>(OperationResult.Updated,
                eventSubscription.Resource);
        }

        var properties = EventSubscriptionSubresourceProperties.From(request);
        var resource = new EventSubscriptionSubresource(subscriptionIdentifier, resourceGroupIdentifier, topicName, eventSubscriptionName, properties);
        
        _provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, eventSubscriptionName,
            topicName, EventSubscriptionSubresource, resource);

        return new ControlPlaneOperationResult<EventSubscriptionSubresource>(OperationResult.Created, resource);
    }

    internal ControlPlaneOperationResult<EventSubscriptionSubresource> GetEventSubscription(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string topicName, string eventSubscriptionName)
    {
        var topicResource= Get(subscriptionIdentifier, resourceGroupIdentifier, topicName);
        if (topicResource.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<EventSubscriptionSubresource>(OperationResult.NotFound, null,
                topicResource.Reason, topicResource.Code);
        }

        var resource = _provider.GetSubresourceAs<EventSubscriptionSubresource>(subscriptionIdentifier,
            resourceGroupIdentifier, eventSubscriptionName, topicName, EventSubscriptionSubresource);
        
        return new ControlPlaneOperationResult<EventSubscriptionSubresource>(OperationResult.Success, resource);
    }

    public ControlPlaneOperationResult DeleteEventSubscription(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string topicName, string eventSubscriptionName)
    {
        var topicResource = Get(subscriptionIdentifier, resourceGroupIdentifier, topicName);
        if (topicResource.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound,
                topicResource.Reason, topicResource.Code);
        }
        
        var eventSubscription= GetEventSubscription(subscriptionIdentifier, resourceGroupIdentifier, topicName,
            eventSubscriptionName);
        if (eventSubscription.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, 
                eventSubscription.Reason, eventSubscription.Code);
        }

        _provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, eventSubscriptionName,
            eventSubscriptionName, EventSubscriptionSubresource);
        
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<EventSubscriptionSubresource[]> ListEventSubscriptions(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string topicName, string? topFilter)
    {
        var topicResource = Get(subscriptionIdentifier, resourceGroupIdentifier, topicName);
        if (topicResource.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<EventSubscriptionSubresource[]>(OperationResult.NotFound, null,
                topicResource.Reason, topicResource.Code);
        }
        
        var resources = _provider
            .ListSubresourcesAs<EventSubscriptionSubresource>(subscriptionIdentifier, resourceGroupIdentifier, topicName, EventSubscriptionSubresource);

        if (!string.IsNullOrWhiteSpace(topFilter))
        {
            resources = [.. resources.Take(int.Parse(topFilter))];
        }

        return new ControlPlaneOperationResult<EventSubscriptionSubresource[]>(OperationResult.Success, [.. resources]);
    }

    public ControlPlaneOperationResult<EventSubscriptionSubresource> UpdateEventSubscription(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string topicName, string eventSubscriptionName, EventSubscriptionSubresourceProperties request)
    {
        var eventSubscription = GetEventSubscription(subscriptionIdentifier, resourceGroupIdentifier, topicName,
            eventSubscriptionName);
        if (eventSubscription.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<EventSubscriptionSubresource>(eventSubscription.Result, null,
                eventSubscription.Reason, eventSubscription.Code);
        }
        
        eventSubscription.Resource!.UpdateFromRequest(request);

        _provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, eventSubscriptionName,
            topicName, EventSubscriptionSubresource, eventSubscription.Resource);
        return new ControlPlaneOperationResult<EventSubscriptionSubresource>(OperationResult.Updated,
            eventSubscription.Resource);
    }

    public ControlPlaneOperationResult<string> GetEventSubscriptionUrl(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string topicName, string eventSubscriptionName)
    {
        var eventSubscription = GetEventSubscription(subscriptionIdentifier, resourceGroupIdentifier, topicName,
            eventSubscriptionName);
        if (eventSubscription.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<string>(eventSubscription.Result, null,
                eventSubscription.Reason, eventSubscription.Code);
        }

        return new ControlPlaneOperationResult<string>(OperationResult.Success,
            $"https://{eventSubscriptionName}.{topicName}.{GlobalSettings.EventGridDnsSuffix}");
    }

    public ControlPlaneOperationResult<DeliveryAttributeMapping[]> GetDeliveryAttributes(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string topicName, string eventSubscriptionName)
    {
        var eventSubscription = GetEventSubscription(subscriptionIdentifier, resourceGroupIdentifier, topicName,
            eventSubscriptionName);
        if (eventSubscription.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<DeliveryAttributeMapping[]>(eventSubscription.Result, null,
                eventSubscription.Reason, eventSubscription.Code);
        }

        var attributes =
            eventSubscription.Resource!.Properties.Destination?.Properties?.DeliveryAttributeMappings?.ToArray() ??
            [];
        
        return new ControlPlaneOperationResult<DeliveryAttributeMapping[]>(OperationResult.Success, attributes);
    }
}