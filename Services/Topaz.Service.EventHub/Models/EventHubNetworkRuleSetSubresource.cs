using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.EventHub.Models;

internal sealed class EventHubNetworkRuleSetSubresource : ArmSubresource<EventHubNetworkRuleSetSubresourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public EventHubNetworkRuleSetSubresource()
#pragma warning restore CS8618
    {
    }

    public EventHubNetworkRuleSetSubresource(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        EventHubNamespaceIdentifier namespaceIdentifier,
        string networkRuleSetName,
        EventHubNetworkRuleSetSubresourceProperties properties)
    {
        Id =
            $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/Microsoft.EventHub/namespaces/{namespaceIdentifier}/networkRuleSets/{networkRuleSetName}";
        Name = networkRuleSetName;
        Properties = properties;
    }

    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.EventHub/namespaces/networkRuleSets";
    public override EventHubNetworkRuleSetSubresourceProperties Properties { get; init; }

    public EventHubNamespaceIdentifier GetNamespace()
    {
        return EventHubNamespaceIdentifier.From(Id.Split("/")[8]);
    }
}