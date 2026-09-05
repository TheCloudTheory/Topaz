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

    public DataPlaneOperationResult PublishEvent(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string topicName, string data, string contentTypeHeaderValue)
    {
        var topicOperation = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, topicName);
        if (topicOperation.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult(OperationResult.NotFound, topicOperation.Reason, topicOperation.Code);
        }

        // Event Grid validates if the provided schema matches the payload
        var inputSchema = topicOperation.Resource!.Properties.InputSchema!;
        var contentType = contentTypeHeaderValue.Split(';')[0];
        if (inputSchema == InputSchema.EventGridSchema && contentType != "application/json" ||
            inputSchema == InputSchema.CloudEventSchemaV1_0 &&
            (contentType != "application/cloudevents+json" &&
             contentType != "application/cloudevents-batch+json"))
        {
            return new DataPlaneOperationResult(OperationResult.BadRequest,
                $"Content-Type must match the topic input schema. Got '{contentType}' for {inputSchema} schema.",
                "BadRequest");
        }

        if (data.Length > 5000)
        {
            return new DataPlaneOperationResult(OperationResult.BadRequest,
                "A batch can contain a maximum of 5,000 events.", "BadRequest");
        }
        
        const uint maxPayloadSizeInMbs = 1024 * 1024;
        var payloadSize = JsonSerializer.SerializeToUtf8Bytes(data).Length;
        if(payloadSize > maxPayloadSizeInMbs)
        {
            return new DataPlaneOperationResult(OperationResult.BadRequest,
                "A batch can contain a maximum of 1 MB.", "BadRequest");
        }

        if (inputSchema == InputSchema.EventGridSchema)
        {
            foreach (var message in JsonSerializer.Deserialize<EventGridEventSchema[]>(data, GlobalSettings.JsonOptions)!)
            {
                _provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, message.Id!, topicName,
                    EventSubresource,EventGridEventEnvelope<EventGridEventSchema>.From(message));
            }
        }

        var isBatch = contentType != "application/cloudevents+json";
        if (inputSchema != InputSchema.CloudEventSchemaV1_0)
            return new DataPlaneOperationResult(OperationResult.Success);
        {
            var events = isBatch
                ? JsonSerializer.Deserialize<EventGridCloudEventSchema[]>(data, GlobalSettings.JsonOptions)
                :
                [
                    JsonSerializer.Deserialize<EventGridCloudEventSchema>(data, GlobalSettings.JsonOptions)!
                ];
            
            foreach (var message in events!)
            {
                _provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, message.Id, topicName,
                    EventSubresource,EventGridEventEnvelope<EventGridEventSchema>.From(message));
            }
        }

        return new DataPlaneOperationResult(OperationResult.Success);
    }
}