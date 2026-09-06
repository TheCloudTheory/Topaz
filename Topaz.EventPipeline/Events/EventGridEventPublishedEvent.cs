namespace Topaz.EventPipeline.Events;

public class EventGridEventPublishedEvent : IEventDefinition<EventGridEventPublishedEventData>
{
    public const string EventName = "EventGridEventPublished";
    
    public string Name => EventName;
    public required EventGridEventPublishedEventData Data { get; init; }
}

public class EventGridEventPublishedEventData
{
    public required string ResourceId { get; init; }
    public required string Subject { get; init; }
    public required string EventType { get; init; }
    public required string DataVersion { get; init; } = "";
    public required string MetadataVersion { get; init; } = "1";
    public required object Data { get; init; }
}