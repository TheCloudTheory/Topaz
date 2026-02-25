namespace Topaz.EventPipeline.Events;

public class SubscriptionCreatedEvent : IEventDefinition<SubscriptionCreatedEventData>
{
    public string Name => "SubscriptionCreated";
    public required SubscriptionCreatedEventData Data { get; init; }
}

public class SubscriptionCreatedEventData
{
    public required string SubscriptionId { get; init; }
}