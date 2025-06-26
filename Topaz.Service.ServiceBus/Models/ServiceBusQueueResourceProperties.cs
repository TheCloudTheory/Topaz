using Topaz.Service.ServiceBus.Models.Requests;

namespace Topaz.Service.ServiceBus.Models;

internal sealed class ServiceBusQueueResourceProperties
{
    public object? CountDetails { get; init; }
    public DateTimeOffset? CreatedOn { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    public DateTimeOffset? AccessedOn { get; init; }
    public long? SizeInBytes { get; init; }
    public long? MessageCount { get; init; }
    public TimeSpan? LockDuration { get; init; }
    public int? MaxSizeInMegabytes { get; init; }
    public long? MaxMessageSizeInKilobytes { get; init; }
    public bool? RequiresDuplicateDetection { get; init; }
    public bool? RequiresSession { get; init; }
    public TimeSpan? DefaultMessageTimeToLive { get; init; }
    public bool? DeadLetteringOnMessageExpiration { get; init; }
    public TimeSpan? DuplicateDetectionHistoryTimeWindow { get; init; }
    public int? MaxDeliveryCount { get; init; }
    public int? Status { get; init; }
    public bool? EnableBatchedOperations { get; init; }
    public TimeSpan? AutoDeleteOnIdle { get; init; }
    public bool? EnablePartitioning { get; init; }
    public bool? EnableExpress { get; init; }
    public string? ForwardTo { get; init; }
    public string? ForwardDeadLetteredMessagesTo { get; init; }

    public static ServiceBusQueueResourceProperties From(CreateOrUpdateServiceBusQueueRequest request)
    {
        return new ServiceBusQueueResourceProperties
        {
            CountDetails = request.Properties.CountDetails,
            SizeInBytes = request.Properties.SizeInBytes,
            MessageCount = request.Properties.MessageCount,
            LockDuration = request.Properties.LockDuration,
            MaxSizeInMegabytes = request.Properties.MaxSizeInMegabytes,
            MaxMessageSizeInKilobytes = request.Properties.MaxMessageSizeInKilobytes,
            RequiresDuplicateDetection = request.Properties.RequiresDuplicateDetection,
            RequiresSession = request.Properties.RequiresSession,
            DeadLetteringOnMessageExpiration = request.Properties.DeadLetteringOnMessageExpiration,
            DuplicateDetectionHistoryTimeWindow = request.Properties.DuplicateDetectionHistoryTimeWindow,
            ForwardTo = request.Properties.ForwardTo,
            ForwardDeadLetteredMessagesTo = request.Properties.ForwardDeadLetteredMessagesTo,
            DefaultMessageTimeToLive = request.Properties.DefaultMessageTimeToLive,
            MaxDeliveryCount = request.Properties.MaxDeliveryCount,
            EnableBatchedOperations = request.Properties.EnableBatchedOperations,
            AutoDeleteOnIdle = request.Properties.AutoDeleteOnIdle,
            EnablePartitioning = request.Properties.EnablePartitioning,
            EnableExpress = request.Properties.EnableExpress,
        };
    }
}