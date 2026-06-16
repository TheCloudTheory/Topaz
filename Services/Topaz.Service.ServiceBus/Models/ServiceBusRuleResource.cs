using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.ServiceBus.Models.Responses;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ServiceBus.Models;

internal sealed class ServiceBusRuleResource : ArmSubresource<ServiceBusRuleResourceProperties>
{
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

    public string ToXmlString()
    {
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(ServiceBusRuleAtomResponse));
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, ServiceBusRuleAtomResponse.From(Name, Properties));
        return stringWriter.ToString();
    }
}
