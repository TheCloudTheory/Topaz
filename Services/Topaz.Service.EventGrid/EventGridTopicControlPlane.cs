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
}