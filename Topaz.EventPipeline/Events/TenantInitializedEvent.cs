namespace Topaz.EventPipeline.Events;

public class TenantInitializedEvent : IEventDefinition<TenantInitializedEventData>
{
    public const string EventName = "TenantInitialized";

    public string Name => EventName;
    public required TenantInitializedEventData Data { get; init; }
}

public class TenantInitializedEventData
{
    public required string TenantId { get; init; }
}
