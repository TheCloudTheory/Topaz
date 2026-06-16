using System.Text.Json.Serialization;
using System.Xml.Linq;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ServiceBus.Models;

internal sealed class ServiceBusRuleResource : ArmSubresource<ServiceBusRuleResourceProperties>
{
    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";
    private static readonly XNamespace Sb = "http://schemas.microsoft.com/netservices/2010/10/servicebus/connect";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    [JsonConstructor]
#pragma warning disable CS8618
    public ServiceBusRuleResource()
#pragma warning restore CS8618
    {
    }

    public ServiceBusRuleResource(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier,
        string topicName,
        string subscriptionName,
        string ruleName,
        ServiceBusRuleResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/Microsoft.ServiceBus/namespaces/{namespaceIdentifier}/topics/{topicName}/subscriptions/{subscriptionName}/rules/{ruleName}";
        Name = ruleName;
        Properties = properties;
    }

    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.ServiceBus/namespaces/topics/subscriptions/rules";
    public override ServiceBusRuleResourceProperties Properties { get; init; }

    public XElement ToEntryElement()
    {
        var filterEl = BuildFilterElement();
        var actionEl = BuildActionElement();

        var ruleDesc = new XElement(Sb + "RuleDescription",
            filterEl,
            actionEl,
            new XElement(Sb + "Name", Name));

        return new XElement(Atom + "entry",
            new XElement(Atom + "title", Name),
            new XElement(Atom + "content",
                new XAttribute("type", "application/xml"),
                ruleDesc));
    }

    public string ToXmlString() => ToEntryElement().ToString();

    private XElement BuildFilterElement()
    {
        return Properties.FilterType switch
        {
            "CorrelationFilter" when Properties.CorrelationFilter != null => new XElement(Sb + "Filter",
                new XAttribute(Xsi + "type", "CorrelationFilter"),
                new XAttribute(XNamespace.Xmlns + "p2", Xsi.NamespaceName),
                Properties.CorrelationFilter.ContentType != null ? new XElement(Sb + "ContentType", Properties.CorrelationFilter.ContentType) : null!,
                Properties.CorrelationFilter.CorrelationId != null ? new XElement(Sb + "CorrelationId", Properties.CorrelationFilter.CorrelationId) : null!,
                Properties.CorrelationFilter.MessageId != null ? new XElement(Sb + "MessageId", Properties.CorrelationFilter.MessageId) : null!,
                Properties.CorrelationFilter.ReplyTo != null ? new XElement(Sb + "ReplyTo", Properties.CorrelationFilter.ReplyTo) : null!,
                Properties.CorrelationFilter.ReplyToSessionId != null ? new XElement(Sb + "ReplyToSessionId", Properties.CorrelationFilter.ReplyToSessionId) : null!,
                Properties.CorrelationFilter.SessionId != null ? new XElement(Sb + "SessionId", Properties.CorrelationFilter.SessionId) : null!,
                Properties.CorrelationFilter.Subject != null ? new XElement(Sb + "Label", Properties.CorrelationFilter.Subject) : null!,
                Properties.CorrelationFilter.To != null ? new XElement(Sb + "To", Properties.CorrelationFilter.To) : null!),
            "True" => new XElement(Sb + "Filter",
                new XAttribute(Xsi + "type", "TrueFilter"),
                new XAttribute(XNamespace.Xmlns + "p2", Xsi.NamespaceName),
                new XElement(Sb + "SqlExpression", "1=1"),
                new XElement(Sb + "Parameters")),
            _ => new XElement(Sb + "Filter",
                new XAttribute(Xsi + "type", "SqlFilter"),
                new XAttribute(XNamespace.Xmlns + "p2", Xsi.NamespaceName),
                new XElement(Sb + "SqlExpression", Properties.SqlFilter?.SqlExpression ?? "1=1"),
                new XElement(Sb + "Parameters"))
        };
    }

    private XElement BuildActionElement()
    {
        if (Properties.Action?.SqlExpression != null)
        {
            return new XElement(Sb + "Action",
                new XAttribute(Xsi + "type", "SqlRuleAction"),
                new XAttribute(XNamespace.Xmlns + "p2", Xsi.NamespaceName),
                new XElement(Sb + "SqlExpression", Properties.Action.SqlExpression),
                new XElement(Sb + "Parameters"),
                new XElement(Sb + "CompatibilityLevel", Properties.Action.CompatibilityLevel));
        }

        return new XElement(Sb + "Action",
            new XAttribute(Xsi + "type", "EmptyRuleAction"),
            new XAttribute(XNamespace.Xmlns + "p2", Xsi.NamespaceName));
    }
}
