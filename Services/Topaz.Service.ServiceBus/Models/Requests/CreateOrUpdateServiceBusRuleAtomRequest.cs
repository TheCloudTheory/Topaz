using System.Xml.Linq;
using JetBrains.Annotations;

namespace Topaz.Service.ServiceBus.Models.Requests;

public sealed class CreateOrUpdateServiceBusRuleAtomRequest
{
    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";
    private static readonly XNamespace Sb = "http://schemas.microsoft.com/netservices/2010/10/servicebus/connect";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    public string? FilterType { get; private set; }
    public string? SqlExpression { get; private set; }
    public string? ActionSqlExpression { get; private set; }

    // Correlation filter fields
    public string? ContentType { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? MessageId { get; private set; }
    public string? ReplyTo { get; private set; }
    public string? ReplyToSessionId { get; private set; }
    public string? SessionId { get; private set; }
    public string? Label { get; private set; }
    public string? To { get; private set; }

    public static CreateOrUpdateServiceBusRuleAtomRequest Parse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var ruleDesc = doc.Descendants(Sb + "RuleDescription").FirstOrDefault()
            ?? doc.Descendants("RuleDescription").FirstOrDefault();

        var filterEl = ruleDesc?.Element(Sb + "Filter") ?? ruleDesc?.Element("Filter");
        var actionEl = ruleDesc?.Element(Sb + "Action") ?? ruleDesc?.Element("Action");

        var filterType = filterEl?.Attribute(Xsi + "type")?.Value ?? "SqlFilter";

        return new CreateOrUpdateServiceBusRuleAtomRequest
        {
            FilterType = filterType,
            SqlExpression = filterEl?.Element(Sb + "SqlExpression")?.Value
                ?? filterEl?.Element("SqlExpression")?.Value,
            ContentType = filterEl?.Element(Sb + "ContentType")?.Value ?? filterEl?.Element("ContentType")?.Value,
            CorrelationId = filterEl?.Element(Sb + "CorrelationId")?.Value ?? filterEl?.Element("CorrelationId")?.Value,
            MessageId = filterEl?.Element(Sb + "MessageId")?.Value ?? filterEl?.Element("MessageId")?.Value,
            ReplyTo = filterEl?.Element(Sb + "ReplyTo")?.Value ?? filterEl?.Element("ReplyTo")?.Value,
            ReplyToSessionId = filterEl?.Element(Sb + "ReplyToSessionId")?.Value ?? filterEl?.Element("ReplyToSessionId")?.Value,
            SessionId = filterEl?.Element(Sb + "SessionId")?.Value ?? filterEl?.Element("SessionId")?.Value,
            Label = filterEl?.Element(Sb + "Label")?.Value ?? filterEl?.Element("Label")?.Value,
            To = filterEl?.Element(Sb + "To")?.Value ?? filterEl?.Element("To")?.Value,
            ActionSqlExpression = actionEl?.Element(Sb + "SqlExpression")?.Value
                ?? actionEl?.Element("SqlExpression")?.Value,
        };
    }

    public static ServiceBusRuleResourceProperties ToProperties(CreateOrUpdateServiceBusRuleAtomRequest request)
    {
        ServiceBusRuleResourceProperties props = request.FilterType switch
        {
            "TrueFilter" => ServiceBusRuleResourceProperties.DefaultTrueFilter(),
            "CorrelationFilter" => ServiceBusRuleResourceProperties.FromCorrelationFilter(
                request.ContentType, request.CorrelationId, request.MessageId,
                request.ReplyTo, request.ReplyToSessionId, request.SessionId,
                request.Label, request.To),
            _ => ServiceBusRuleResourceProperties.FromSqlFilter(request.SqlExpression ?? "1=1")
        };

        if (request.ActionSqlExpression != null)
        {
            props.Action = new ServiceBusSqlRuleAction { SqlExpression = request.ActionSqlExpression };
        }

        return props;
    }
}

