using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Models;

internal sealed class ServiceBusAuthorizationRuleResource
    : ArmSubresource<ServiceBusAuthorizationRuleResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public ServiceBusAuthorizationRuleResource()
#pragma warning restore CS8618
    {
    }

    public ServiceBusAuthorizationRuleResource(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string ruleName,
        ServiceBusAuthorizationRuleResourceProperties properties,
        string armIdSuffix)
    {
        Id = $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/Microsoft.ServiceBus/{armIdSuffix}/authorizationRules/{ruleName}";
        Name = ruleName;
        Properties = properties;
        _type = $"Microsoft.ServiceBus/{armIdSuffix.Split('/')[^2]}/authorizationRules";
    }

    private readonly string _type = "Microsoft.ServiceBus/namespaces/authorizationRules";

    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => _type;
    public override ServiceBusAuthorizationRuleResourceProperties Properties { get; init; }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
