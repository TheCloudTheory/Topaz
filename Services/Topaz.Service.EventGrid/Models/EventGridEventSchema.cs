using Topaz.EventPipeline.Events;

namespace Topaz.Service.EventGrid.Models;

internal sealed class EventGridEventSchema
{
    public string? Id { get; init; }
    public string? Subject { get; init; }
    public string? Topic { get; init; }
    public string? EventType { get; init; }
    public string? EventTime { get; init; }
    public object? Data { get; init; }
    public string? DataVersion { get; init; }
    public string? MetadataVersion { get; init; }

    public static EventGridEventSchema From(EventGridEventPublishedEventData message)
    {
        return new EventGridEventSchema
        {
            Id = Guid.NewGuid().ToString(),
            Subject = message.Subject,
            Topic = message.ResourceId,
            EventType = message.EventType,
            EventTime = DateTimeOffset.Now.ToString("O"),
            Data = message.Data,
            DataVersion = message.DataVersion,
            MetadataVersion = message.MetadataVersion
        };
    }
}