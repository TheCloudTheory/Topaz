using System.Security.Cryptography;
using Topaz.Service.Shared;

namespace Topaz.Service.EventGrid.Models;

internal sealed class EventGridValidationEvent : TopazApiModel
{
    public string? Id { get; init; }
    public string? Subject { get; init; } = "";
    public string? Topic { get; init; }
    public string? EventTime { get; init; }
    public EventGridValidationEventPayload? Data { get; init; }

    internal class EventGridValidationEventPayload
    {
        public required string ValidationCode { get; init; }
        public string? ValidationUrl { get; init; }
    }

    public string? DataVersion { get; init; } = "1";
    public string? MetadataVersion { get; init; } = "1";
    public string EventType { get; } = "Microsoft.EventGrid.SubscriptionValidationEvent";
    

    public static EventGridValidationEvent New(string topicId)
    {
        return new EventGridValidationEvent
        {
            Id = Guid.NewGuid().ToString(),
            Topic = topicId,
            EventTime = DateTime.UtcNow.ToString("o"),
            Data = new EventGridValidationEventPayload()
            {
                ValidationCode = Convert.ToBase64String(RandomNumberGenerator.GetBytes(8)),
                ValidationUrl = ""
            }
        };
    }
}