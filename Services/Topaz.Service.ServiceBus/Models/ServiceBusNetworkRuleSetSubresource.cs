using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ServiceBus.Models;

internal sealed class ServiceBusNetworkRuleSetSubresource : ArmSubresource<ServiceBusNetworkRuleSetSubresourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public ServiceBusNetworkRuleSetSubresource()
#pragma warning restore CS8618
    {
    }

    public ServiceBusNetworkRuleSetSubresource(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier,
        string networkRuleSetName,
        ServiceBusNetworkRuleSetSubresourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/Microsoft.ServiceBus/namespaces/{namespaceIdentifier}/networkRuleSets/{networkRuleSetName}";
        Name = networkRuleSetName;
        Properties = properties;
    }

    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.ServiceBus/namespaces/networkRuleSets";
    public override ServiceBusNetworkRuleSetSubresourceProperties Properties { get; init; }

    public ServiceBusNamespaceIdentifier GetNamespace()
    {
        return ServiceBusNamespaceIdentifier.From(Id.Split("/")[8]);
    }
}
