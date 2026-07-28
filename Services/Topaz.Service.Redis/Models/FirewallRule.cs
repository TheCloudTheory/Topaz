using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Topaz.Service.Redis.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.Redis.Models;

internal sealed class FirewallRule : TopazApiModel, IValidatable
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    [UsedImplicitly] public string Type => "Microsoft.Cache/Redis/firewallRules";
    
    public FirewallRuleProperties? Properties { get; init; }

    [UsedImplicitly]
    internal class FirewallRuleProperties
    {
        [JsonPropertyName("startIP")]
        public string? StartIp { get; set; }
        
        [JsonPropertyName("endIP")]
        public string? EndIp { get; set; }
    }

    public static FirewallRule FromRequest(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string cacheName, string ruleName, CreateOrUpdateFirewallRuleRequest request)
    {
        return new FirewallRule
        {
            Id =
                $"/subscriptions/{subscriptionIdentifier.Value}/resourceGroups/{resourceGroupIdentifier.Value}/providers/Microsoft.Cache/Redis/{cacheName}/firewallRules/{ruleName}",
            Name = ruleName,
            Properties = new FirewallRuleProperties
            {
                StartIp = request.Properties?.StartIp,
                EndIp = request.Properties?.EndIp,
            }
        };
    }

    public void UpdateFromRequest(CreateOrUpdateFirewallRuleRequest request)
    {
        Properties!.StartIp = request.Properties?.StartIp;
        Properties!.EndIp = request.Properties?.EndIp;
    }

    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        if(Properties == null)
        {
            return (false, "Properties cannot be null");
        }
        
        if(string.IsNullOrWhiteSpace(Properties.StartIp) || string.IsNullOrWhiteSpace(Properties.EndIp))
        {
            return (false, "StartIp and EndIp cannot be null or whitespace");
        }

        return (true, null);
    }
}