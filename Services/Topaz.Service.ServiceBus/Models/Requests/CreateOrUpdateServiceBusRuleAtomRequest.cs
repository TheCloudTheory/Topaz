using System.Xml.Serialization;
using JetBrains.Annotations;

namespace Topaz.Service.ServiceBus.Models.Requests;

[XmlRoot("entry", Namespace = "http://www.w3.org/2005/Atom", IsNullable = false)]
internal sealed class CreateOrUpdateServiceBusRuleAtomRequest
{
    [XmlElement("content")]
    public RuleRequestContent? Content { get; init; } = new();

    public class RuleRequestContent
    {
        [XmlElement("RuleDescription", Namespace = "http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
        public RuleDescriptionData? RuleDescription { get; init; } = new();
    }

    [UsedImplicitly]
    public class RuleDescriptionData
    {
        [XmlElement("Name")]
        public string? Name { get; set; }

        [XmlElement("Filter")]
        public RuleFilterData? Filter { get; set; }

        [XmlElement("Action")]
        public RuleActionData? Action { get; set; }
    }

    [UsedImplicitly]
    public class RuleFilterData
    {
        // xsi:type discriminator from the SDK: "SqlFilter", "CorrelationFilter", "TrueFilter"
        [XmlAttribute("type", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string? Type { get; set; }

        [XmlElement("SqlExpression")]
        public string? SqlExpression { get; set; }

        // Correlation filter fields
        [XmlElement("ContentType")] public string? ContentType { get; set; }
        [XmlElement("CorrelationId")] public string? CorrelationId { get; set; }
        [XmlElement("MessageId")] public string? MessageId { get; set; }
        [XmlElement("ReplyTo")] public string? ReplyTo { get; set; }
        [XmlElement("ReplyToSessionId")] public string? ReplyToSessionId { get; set; }
        [XmlElement("SessionId")] public string? SessionId { get; set; }
        [XmlElement("Label")] public string? Label { get; set; }
        [XmlElement("To")] public string? To { get; set; }
    }

    [UsedImplicitly]
    public class RuleActionData
    {
        [XmlAttribute("type", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string? Type { get; set; }

        [XmlElement("SqlExpression")]
        public string? SqlExpression { get; set; }
    }

    public static ServiceBusRuleResourceProperties ToProperties(CreateOrUpdateServiceBusRuleAtomRequest request)
    {
        var filter = request.Content?.RuleDescription?.Filter;
        var action = request.Content?.RuleDescription?.Action;

        ServiceBusRuleResourceProperties props = filter?.Type switch
        {
            "TrueFilter" => ServiceBusRuleResourceProperties.DefaultTrueFilter(),
            "CorrelationFilter" => ServiceBusRuleResourceProperties.FromCorrelationFilter(
                filter.ContentType, filter.CorrelationId, filter.MessageId,
                filter.ReplyTo, filter.ReplyToSessionId, filter.SessionId,
                filter.Label, filter.To),
            _ => ServiceBusRuleResourceProperties.FromSqlFilter(filter?.SqlExpression ?? "1=1")
        };

        if (action?.SqlExpression != null)
        {
            props.Action = new ServiceBusSqlRuleAction { SqlExpression = action.SqlExpression };
        }

        return props;
    }
}

