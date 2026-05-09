namespace Topaz.Portal.Models.ServiceBus;

public sealed class ServiceBusNamespaceDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Location { get; set; }
    public string? ResourceGroupName { get; set; }
    public string? SubscriptionId { get; set; }
    public string? SubscriptionName { get; set; }
    public string? ProvisioningState { get; set; }
    public string? Status { get; set; }
    public string? ServiceBusEndpoint { get; set; }
    public string? MetricId { get; set; }
    public string? SkuName { get; set; }
    public string? SkuTier { get; set; }
    public int? SkuCapacity { get; set; }
    public bool? IsZoneRedundant { get; set; }
    public string? MinimumTlsVersion { get; set; }
    public bool? DisableLocalAuth { get; set; }
    public int? PremiumMessagingPartitions { get; set; }
    public DateTimeOffset? CreatedOn { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    public Dictionary<string, string> Tags { get; set; } = [];
}

public sealed class ListServiceBusNamespacesResponse
{
    public ServiceBusNamespaceDto[] Value { get; set; } = [];
}

public sealed class ServiceBusQueueDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? NamespaceName { get; set; }
    public string? Status { get; set; }
    public long? MessageCount { get; set; }
    public long? SizeInBytes { get; set; }
    public int? MaxDeliveryCount { get; set; }
    public int? MaxSizeInMegabytes { get; set; }
    public bool? RequiresSession { get; set; }
    public bool? RequiresDuplicateDetection { get; set; }
    public DateTimeOffset? CreatedOn { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
}

public sealed class ListServiceBusQueuesResponse
{
    public ServiceBusQueueDto[] Value { get; set; } = [];
}

public sealed class ServiceBusTopicDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? NamespaceName { get; set; }
    public string? Status { get; set; }
    public long? SizeInBytes { get; set; }
    public int? SubscriptionCount { get; set; }
    public int? MaxSizeInMegabytes { get; set; }
    public bool? RequiresDuplicateDetection { get; set; }
    public DateTimeOffset? CreatedOn { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
}

public sealed class ListServiceBusTopicsResponse
{
    public ServiceBusTopicDto[] Value { get; set; } = [];
}
