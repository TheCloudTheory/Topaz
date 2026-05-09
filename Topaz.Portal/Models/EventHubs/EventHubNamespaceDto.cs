namespace Topaz.Portal.Models.EventHubs;

public sealed class EventHubNamespaceDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Location { get; init; }
    public string? ResourceGroupName { get; init; }
    public string? SubscriptionId { get; init; }
    public string? SubscriptionName { get; init; }
    public string? ProvisioningState { get; init; }
    public string? ServiceBusEndpoint { get; init; }
    public string? Status { get; init; }
    public bool? KafkaEnabled { get; init; }
    public bool? ZoneRedundant { get; init; }
    public bool? IsAutoInflateEnabled { get; init; }
    public int? MaximumThroughputUnits { get; init; }
    public string? MinimumTlsVersion { get; init; }
    public string? SkuName { get; init; }
    public string? SkuTier { get; init; }
    public int? SkuCapacity { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ListEventHubNamespacesResponse
{
    public EventHubNamespaceDto[] Value { get; init; } = [];
}

public sealed class EventHubDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? NamespaceName { get; init; }
    public string? Status { get; init; }
    public int PartitionCount { get; init; }
    public int MessageRetentionInDays { get; init; }
    public IReadOnlyList<string> PartitionIds { get; init; } = [];
    public DateTimeOffset? CreatedOn { get; init; }
    public DateTimeOffset? UpdatedOn { get; init; }
}

public sealed class ListEventHubsResponse
{
    public EventHubDto[] Value { get; init; } = [];
}
