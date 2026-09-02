using System.Text.Json.Serialization;
using Topaz.Service.Shared;

namespace Topaz.Service.EventGrid.Models;

internal sealed class EventSubscriptionSubresourceProperties
{
    public string? Topic { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProvisioningState ProvisioningState { get; init; } = ProvisioningState.Succeeded;

    public EventSubscriptionDestination? Destination { get; set; }
    public DeadLetterDestination? DeadLetterDestination { get; set; }
    public DeliveryWithResourceIdentity? DeliveryWithResourceIdentity { get; set; }
    public DeadLetterWithResourceIdentity? DeadLetterWithResourceIdentity { get; set; }
    public EventSubscriptionFilter? Filter { get; set; }
    public List<string>? Labels { get; set; }
    public string? ExpirationTimeUtc { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EventDeliverySchema EventDeliverySchema { get; set; } = EventDeliverySchema.EventGridSchema;

    public RetryPolicy? RetryPolicy { get; set; }

    public void UpdateFromRequest(EventSubscriptionSubresourceProperties request)
    {
        Topic = request.Topic ?? Topic;
        Destination = request.Destination ?? Destination;
        DeadLetterDestination = request.DeadLetterDestination ?? DeadLetterDestination;
        DeliveryWithResourceIdentity = request.DeliveryWithResourceIdentity ?? DeliveryWithResourceIdentity;
        DeadLetterWithResourceIdentity = request.DeadLetterWithResourceIdentity ?? DeadLetterWithResourceIdentity;
        Filter = request.Filter ?? Filter;
        Labels = request.Labels ?? Labels;
        ExpirationTimeUtc = request.ExpirationTimeUtc ?? ExpirationTimeUtc;
        EventDeliverySchema = request.EventDeliverySchema;
        RetryPolicy = request.RetryPolicy ?? RetryPolicy;
    }

    public static EventSubscriptionSubresourceProperties From(EventSubscriptionSubresourceProperties request)
    {
        return new EventSubscriptionSubresourceProperties
        {
            Topic = request.Topic,
            Destination = request.Destination,
            DeadLetterDestination = request.DeadLetterDestination,
            DeliveryWithResourceIdentity = request.DeliveryWithResourceIdentity,
            DeadLetterWithResourceIdentity = request.DeadLetterWithResourceIdentity,
            Filter = request.Filter,
            Labels = request.Labels,
            ExpirationTimeUtc = request.ExpirationTimeUtc,
            EventDeliverySchema = request.EventDeliverySchema,
            RetryPolicy = request.RetryPolicy,
        };
    }
}

internal enum EventDeliverySchema
{
    EventGridSchema,
    CustomInputSchema,
    CloudEventSchemaV1_0,
}

internal sealed class RetryPolicy
{
    public int? MaxDeliveryAttempts { get; set; } = 30;
    public int? EventTimeToLiveInMinutes { get; set; } = 1440;
}

internal sealed class EventSubscriptionFilter
{
    public List<object>? AdvancedFilters { get; set; }
    public bool? EnableAdvancedFilteringOnArrays { get; set; }
    public List<string>? IncludedEventTypes { get; set; }
    public bool? IsSubjectCaseSensitive { get; set; } = false;
    public string? SubjectBeginsWith { get; set; }
    public string? SubjectEndsWith { get; set; }
}

internal sealed class EventSubscriptionIdentity
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EventSubscriptionIdentityType? Type { get; set; }

    public string? UserAssignedIdentity { get; set; }
}

internal enum EventSubscriptionIdentityType
{
    SystemAssigned,
    UserAssigned,
}

internal sealed class DeliveryWithResourceIdentity
{
    public EventSubscriptionDestination? Destination { get; set; }
    public EventSubscriptionIdentity? Identity { get; set; }
}

internal sealed class DeadLetterWithResourceIdentity
{
    public DeadLetterDestination? DeadLetterDestination { get; set; }
    public EventSubscriptionIdentity? Identity { get; set; }
}

internal sealed class DeadLetterDestination
{
    public string EndpointType { get; set; } = "StorageBlob";
    public StorageBlobDeadLetterDestinationProperties? Properties { get; set; }
}

internal sealed class StorageBlobDeadLetterDestinationProperties
{
    public string? ResourceId { get; set; }
    public string? BlobContainerName { get; set; }
}

internal sealed class EventSubscriptionDestination
{
    public string EndpointType { get; set; } = string.Empty;
    public EventSubscriptionDestinationProperties? Properties { get; set; }
}

internal sealed class EventSubscriptionDestinationProperties
{
    public string? ResourceId { get; set; }
    public string? EndpointUrl { get; set; }
    public string? EndpointBaseUrl { get; set; }
    public string? QueueName { get; set; }
    public long? QueueMessageTimeToLiveInSeconds { get; set; }
    public int? MaxEventsPerBatch { get; set; } = 1;
    public int? PreferredBatchSizeInKilobytes { get; set; } = 64;
    public List<DeliveryAttributeMapping>? DeliveryAttributeMappings { get; set; }
    public string? AzureActiveDirectoryTenantId { get; set; }
    public string? AzureActiveDirectoryApplicationIdOrUri { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TlsVersion? MinimumTlsVersionAllowed { get; set; }

    public List<string>? ActionGroups { get; set; }
    public string? Description { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MonitorAlertSeverity? Severity { get; set; }
}

internal enum MonitorAlertSeverity
{
    Sev0,
    Sev1,
    Sev2,
    Sev3,
    Sev4,
}

internal sealed class DeliveryAttributeMapping
{
    public string? Name { get; set; }
    public string Type { get; set; } = string.Empty;
    public DeliveryAttributeMappingProperties? Properties { get; set; }
}

internal sealed class DeliveryAttributeMappingProperties
{
    public string? SourceField { get; set; }
    public string? Value { get; set; }
    public bool? IsSecret { get; set; } = false;
}