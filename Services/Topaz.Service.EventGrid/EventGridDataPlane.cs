using System.Text.Json;
using Topaz.Service.EventGrid.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.EventGrid;

internal sealed class EventGridDataPlane(
    EventGridTopicControlPlane controlPlane,
    ITopazLogger logger)
{
    private static readonly string EventSubresource =
        nameof(Subresource.Events).ToLowerInvariant();
    
    public static EventGridDataPlane New(EventGridTopicControlPlane controlPlane,
        ITopazLogger logger) => new(controlPlane, logger);
    
    private readonly EventGridTopicResourceProvider _provider = new(logger);

    public DataPlaneOperationResult PublishEventGridEvent(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string topicName, EventGridEventSchema[] data)
    {
        var topicOperation = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, topicName);
        if (topicOperation.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult(OperationResult.NotFound, topicOperation.Reason, topicOperation.Code);
        }

        if (data.Length > 5000)
        {
            return new DataPlaneOperationResult(OperationResult.Conflict,
                "A batch can contain a maximum of 5,000 events.", "Conflict");
        }
        
        const uint maxPayloadSizeInMbs = 1024 * 1024;
        var payloadSize = JsonSerializer.SerializeToUtf8Bytes(data).Length;
        if(payloadSize > maxPayloadSizeInMbs)
        {
            return new DataPlaneOperationResult(OperationResult.Conflict,
                "A batch can contain a maximum of 1 MB.", "Conflict");
        }

        foreach (var message in data)
        {
            _provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, message.Id!, topicName,
                EventSubresource,EventGridEventEnvelope<EventGridEventSchema>.From(message));
        }
        
        return new DataPlaneOperationResult(OperationResult.Success);
    }

    public DataPlaneOperationResult PublishCloudEvent(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string topicName, EventGridCloudEventSchema[] data)
    {
        var topicOperation = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, topicName);
        if (topicOperation.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult(OperationResult.NotFound, topicOperation.Reason, topicOperation.Code);
        }

        if (data.Length > 5000)
        {
            return new DataPlaneOperationResult(OperationResult.Conflict,
                "A batch can contain a maximum of 5,000 events.", "Conflict");
        }
        
        const uint maxPayloadSizeInMbs = 1024 * 1024;
        var payloadSize = JsonSerializer.SerializeToUtf8Bytes(data).Length;
        if(payloadSize > maxPayloadSizeInMbs)
        {
            return new DataPlaneOperationResult(OperationResult.Conflict,
                "A batch can contain a maximum of 1 MB.", "Conflict");
        }

        foreach (var message in data)
        {
            _provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, message.Id, topicName,
                EventSubresource,EventGridEventEnvelope<EventGridCloudEventSchema>.From(message));
        }
        
        return new DataPlaneOperationResult(OperationResult.Success);
    }
}