using System.Xml.Serialization;
using JetBrains.Annotations;

namespace Topaz.Service.ServiceBus.Models.Requests;

[UsedImplicitly]
public class CreateOrUpdateServiceBusTopicRequestProperties
{
    [XmlElement("CountDetails")]
    public object? CountDetails { get; init; }
    [XmlElement("SizeInBytes")]
    public long? SizeInBytes { get; init; }
    [XmlElement("AutoDeleteOnIdle")]
    public TimeSpan? AutoDeleteOnIdle { get; init; }
    [XmlElement("DefaultMessageTimeToLive")]
    public TimeSpan? DefaultMessageTimeToLive { get; init; }
    [XmlElement("DuplicateDetectionHistoryTimeWindow")]
    public TimeSpan? DuplicateDetectionHistoryTimeWindow { get; init; }
    [XmlElement("EnableBatchedOperations")]
    public bool? EnableBatchedOperations { get; init; }
    [XmlElement("EnableExpress")]
    public bool? EnableExpress { get; init; }
    [XmlElement("EnablePartitioning")]
    public bool? EnablePartitioning { get; init; }
    [XmlElement("MaxMessageSizeInKilobytes")]
    public long? MaxMessageSizeInKilobytes { get; init; }
    [XmlElement("MaxSizeInMegabytes")]
    public int? MaxSizeInMegabytes { get; init; }
    [XmlElement("RequiresDuplicateDetection")]
    public bool? RequiresDuplicateDetection { get; init; }
    [XmlElement("Status")]
    public string? Status { get; init; }
    [XmlElement("SupportOrdering")]
    public bool? SupportOrdering { get; init; }
}