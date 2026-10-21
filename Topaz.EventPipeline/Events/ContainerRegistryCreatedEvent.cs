namespace Topaz.EventPipeline.Events;

public sealed class ContainerRegistryCreatedEvent : IEventDefinition<ContainerRegistryCreatedEventData>
{
    public const string EventName = "ContainerRegistryCreated";
    
    public string Name => EventName;
    
    public required ContainerRegistryCreatedEventData Data { get; init; }
}

public sealed class ContainerRegistryCreatedEventData
{
    public required Guid? SubscriptionIdentifier { get; init; }
    public required string? ResourceGroupIdentifier { get; init; }
    public required string? RegistryName { get; init; }
    public required string? Location { get; init; }
}