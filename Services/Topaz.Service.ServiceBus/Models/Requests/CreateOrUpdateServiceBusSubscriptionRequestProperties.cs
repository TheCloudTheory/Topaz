using System.Xml.Serialization;
using JetBrains.Annotations;

namespace Topaz.Service.ServiceBus.Models.Requests;

[UsedImplicitly]
public class CreateOrUpdateServiceBusSubscriptionRequestProperties
{
    [XmlElement("LockDuration")] public TimeSpan? LockDuration { get; init; }
    [XmlElement("MaxDeliveryCount")] public int? MaxDeliveryCount { get; init; }
    [XmlElement("EnableBatchedOperations")] public bool? EnableBatchedOperations { get; init; } = true;
    [XmlElement("Status")] public string? Status { get; init; }
    [XmlElement("RequireSession")] public bool RequireSession { get; init; }
    [XmlElement("DeadLetteringOnMessageExpiration")] public bool DeadLetteringOnMessageExpiration { get; init; }
    [XmlElement("DeadLetteringOnFilterEvaluationExceptions")]
    public bool DeadLetteringOnFilterEvaluationExceptions { get; init; } = true;
    
    [XmlElement("DefaultRuleDescription")] public DefaultRuleDescriptionData? DefaultRuleDescription { get; init; }

    public class DefaultRuleDescriptionData
    {
        [XmlElement("Name")] public string? Name { get; set; }
        [XmlElement("Filter", Namespace = "http://www.w3.org/2001/XMLSchema-instance")] public FilterData? Filter { get; set; }
        
        public class FilterData
        {
            [XmlAttribute("type")] public string? Name { get; set; }
            [XmlElement("SqlExpression")] public string? SqlExpression { get; set; }
        }
    }
}