namespace Topaz.EventPipeline.Events;

public class SubscriptionCreatedEvent : IEventDefinition<SubscriptionCreatedEventData>
{
    public const string EventName = "SubscriptionCreated";
    
    public string Name => EventName;
    public required SubscriptionCreatedEventData Data { get; init; }
}

public class SubscriptionCreatedEventData
{
    public required string SubscriptionId { get; init; }
}