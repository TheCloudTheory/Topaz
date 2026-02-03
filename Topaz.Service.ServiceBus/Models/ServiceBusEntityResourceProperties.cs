using System.Xml;
using JetBrains.Annotations;

namespace Topaz.Service.ServiceBus.Models;

public abstract class ServiceBusEntityResourceProperties
{
    public object? CountDetails { [UsedImplicitly] get; set; }
    public DateTimeOffset? CreatedOn { [UsedImplicitly] get; set; }
    public DateTimeOffset? UpdatedOn { [UsedImplicitly] get; set; }
    public DateTimeOffset? AccessedOn { get; set; } = DateTimeOffset.UtcNow;
    public long? SizeInBytes { [UsedImplicitly] get; set; } = 0;
    public long? MessageCount { [UsedImplicitly] get; set; } = 0;
    public TimeSpan? LockDuration { [UsedImplicitly] get; set; }
    public int? MaxSizeInMegabytes { [UsedImplicitly] get; set; }
    public long? MaxMessageSizeInKilobytes { [UsedImplicitly] get; set; } = 0;
    public bool? RequiresDuplicateDetection { [UsedImplicitly] get; set; }
    public bool? RequiresSession { [UsedImplicitly] get; set; } = false;
    public string? DefaultMessageTimeToLive { [UsedImplicitly] get; set; } = XmlConvert.ToString(TimeSpan.MaxValue);
    public bool? DeadLetteringOnMessageExpiration { [UsedImplicitly] get; set; } = false;
    public string? DuplicateDetectionHistoryTimeWindow { [UsedImplicitly] get; set; } =
        XmlConvert.ToString(TimeSpan.FromMinutes(10));
    public int? MaxDeliveryCount { [UsedImplicitly] get; set; }
    public string? Status { [UsedImplicitly] get; set; }
    public bool? EnableBatchedOperations { [UsedImplicitly] get; set; } = false;
    public string? AutoDeleteOnIdle { [UsedImplicitly] get; set; } = XmlConvert.ToString(TimeSpan.MaxValue);
    public bool? EnablePartitioning { [UsedImplicitly] get; set; } = false;
    public bool? EnableExpress { [UsedImplicitly] get; set; } = false;
    public string? ForwardTo { [UsedImplicitly] get; set; }
    public string? ForwardDeadLetteredMessagesTo { [UsedImplicitly] get; set; }
}