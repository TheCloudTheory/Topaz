using System.Xml.Serialization;
using JetBrains.Annotations;

namespace Topaz.Service.ServiceBus.Models.Requests;

[UsedImplicitly]
public class CreateOrUpdateServiceBusQueueRequestProperties
{
    [XmlElement("CountDetails")]
    public object? CountDetails { get; init; }
    [XmlElement("SizeInBytes")]
    public long? SizeInBytes { get; init; }
    [XmlElement("MessageCount")]
    public long? MessageCount { get; init; }
    [XmlElement("LockDuration")]
    public TimeSpan? LockDuration { get; init; }
    [XmlElement("MaxSizeInMegabytes")]
    public int? MaxSizeInMegabytes { get; init; }
    [XmlElement("MaxMessageSizeInKilobytes")]
    public long? MaxMessageSizeInKilobytes { get; init; }
    [XmlElement("RequiresDuplicateDetection")]
    public bool? RequiresDuplicateDetection { get; init; }
    [XmlElement("RequiresSession")]
    public bool? RequiresSession { get; init; }
    [XmlElement("DefaultMessageTimeToLive")]
    public TimeSpan? DefaultMessageTimeToLive { get; init; }
    [XmlElement("DeadLetteringOnMessageExpiration")]
    public bool? DeadLetteringOnMessageExpiration { get; init; }
    [XmlElement("DuplicateDetectionHistoryTimeWindow")]
    public TimeSpan? DuplicateDetectionHistoryTimeWindow { get; init; }
    [XmlElement("MaxDeliveryCount")]
    public int? MaxDeliveryCount { get; init; }
    [XmlElement("EnableBatchedOperations")]
    public bool? EnableBatchedOperations { get; init; }
    [XmlElement("AutoDeleteOnIdle")]
    public TimeSpan? AutoDeleteOnIdle { get; init; }
    [XmlElement("EnablePartitioning")]
    public bool? EnablePartitioning { get; init; }
    [XmlElement("EnableExpress")]
    public bool? EnableExpress { get; init; }
    [XmlElement("ForwardTo")]
    public string? ForwardTo { get; init; }
    [XmlElement("ForwardDeadLetteredMessagesTo")]
    public string? ForwardDeadLetteredMessagesTo { get; init; }
    [XmlElement("Status")]
    public string? Status { get; init; }
}