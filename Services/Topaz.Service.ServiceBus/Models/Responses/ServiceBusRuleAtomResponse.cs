using System.Xml.Serialization;
using Topaz.Service.ServiceBus.Models;

namespace Topaz.Service.ServiceBus.Models.Responses;

[XmlRoot("entry", Namespace = "http://www.w3.org/2005/Atom", IsNullable = false)]
internal sealed class ServiceBusRuleAtomResponse
{
    [XmlElement("title")]
    public string? Title { get; init; }

    [XmlElement("content")]
    public RuleResponseContent? Content { get; init; } = new();

    public class RuleResponseContent
    {
        [XmlElement("RuleDescription", Namespace = "http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
        public RuleDescriptionData? RuleDescription { get; init; } = new();
    }

    public class RuleDescriptionData
    {
        [XmlElement("Name")]
        public string? Name { get; set; }

        [XmlElement("Filter")]
        public RuleFilterData? Filter { get; set; }

        [XmlElement("Action")]
        public RuleActionData? Action { get; set; }
    }

    public class RuleFilterData
    {
        [XmlAttribute("type", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string? Type { get; set; }

        [XmlElement("SqlExpression")]
        public string? SqlExpression { get; set; }

        [XmlElement("CompatibilityLevel")]
        public int? CompatibilityLevel { get; set; }

        [XmlElement("ContentType")] public string? ContentType { get; set; }
        [XmlElement("CorrelationId")] public string? CorrelationId { get; set; }
        [XmlElement("MessageId")] public string? MessageId { get; set; }
        [XmlElement("ReplyTo")] public string? ReplyTo { get; set; }
        [XmlElement("ReplyToSessionId")] public string? ReplyToSessionId { get; set; }
        [XmlElement("SessionId")] public string? SessionId { get; set; }
        [XmlElement("Label")] public string? Label { get; set; }
        [XmlElement("To")] public string? To { get; set; }
    }

    public class RuleActionData
    {
        [XmlAttribute("type", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string? Type { get; set; }

        [XmlElement("SqlExpression")]
        public string? SqlExpression { get; set; }

        [XmlElement("CompatibilityLevel")]
        public int? CompatibilityLevel { get; set; }
    }

    public static ServiceBusRuleAtomResponse From(string ruleName, ServiceBusRuleResourceProperties properties)
    {
        RuleFilterData filter = properties.FilterType switch
        {
            "CorrelationFilter" when properties.CorrelationFilter != null => new RuleFilterData
            {
                Type = "CorrelationFilter",
                ContentType = properties.CorrelationFilter.ContentType,
                CorrelationId = properties.CorrelationFilter.CorrelationId,
                MessageId = properties.CorrelationFilter.MessageId,
                ReplyTo = properties.CorrelationFilter.ReplyTo,
                ReplyToSessionId = properties.CorrelationFilter.ReplyToSessionId,
                SessionId = properties.CorrelationFilter.SessionId,
                Label = properties.CorrelationFilter.Subject,
                To = properties.CorrelationFilter.To
            },
            "True" => new RuleFilterData { Type = "TrueFilter" },
            _ => new RuleFilterData
            {
                Type = "SqlFilter",
                SqlExpression = properties.SqlFilter?.SqlExpression ?? "1=1",
                CompatibilityLevel = properties.SqlFilter?.CompatibilityLevel ?? 20
            }
        };

        RuleActionData action = properties.Action?.SqlExpression != null
            ? new RuleActionData
            {
                Type = "SqlRuleAction",
                SqlExpression = properties.Action.SqlExpression,
                CompatibilityLevel = properties.Action.CompatibilityLevel
            }
            : new RuleActionData { Type = "EmptyRuleAction" };

        return new ServiceBusRuleAtomResponse
        {
            Title = ruleName,
            Content = new RuleResponseContent
            {
                RuleDescription = new RuleDescriptionData
                {
                    Name = ruleName,
                    Filter = filter,
                    Action = action
                }
            }
        };
    }

    public static string FeedFrom(string[] ruleXmls)
    {
        var entries = string.Join("\n", ruleXmls);
        return $"""<?xml version="1.0" encoding="utf-8"?><feed xmlns="http://www.w3.org/2005/Atom">{entries}</feed>""";
    }
}
