using Topaz.Service.Shared;

namespace Topaz.Service.Redis.Models.Responses;

internal sealed class ListFirewallRulesResponse : TopazApiModel
{
    public List<FirewallRule> Value { get; set; } = [];

    public static ListFirewallRulesResponse From(FirewallRule[] rules)
    {
        return new ListFirewallRulesResponse { Value = rules.ToList() };
    }
}