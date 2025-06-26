using JetBrains.Annotations;

namespace Topaz.Service.ServiceBus.Models.Requests;

internal sealed class CreateOrUpdateServiceBusQueueRequest
{
    public CreateOrUpdateServiceBusQueueRequestProperties Properties { get; init; } = new();
    
    [UsedImplicitly]
    public class CreateOrUpdateServiceBusQueueRequestProperties
    {
        public object? CountDetails { get; init; }
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
        public bool? EnableBatchedOperations { get; init; }
        public TimeSpan? AutoDeleteOnIdle { get; init; }
        public bool? EnablePartitioning { get; init; }
        public bool? EnableExpress { get; init; }
        public string? ForwardTo { get; init; }
        public string? ForwardDeadLetteredMessagesTo { get; init; }
    }
}