using Topaz.Service.EventHub.Models.Requests;

namespace Topaz.Service.EventHub.Models;

internal sealed class EventHubNetworkRuleSetSubresourceProperties
{
    public string DefaultAction { get; set; } = "Allow";
    public string PublicNetworkAccess { get; set; } = "Enabled";
    public bool TrustedServiceAccessEnabled { get; set; }
    public IReadOnlyList<VirtualNetworkRule> VirtualNetworkRules { get; set; } = [];
    public IReadOnlyList<IpRule> IpRules { get; set; } = [];

    public static EventHubNetworkRuleSetSubresourceProperties Default()
    {
        return new EventHubNetworkRuleSetSubresourceProperties();
    }

    public static EventHubNetworkRuleSetSubresourceProperties From(CreateOrUpdateNamespaceNetworkRuleSetRequest request)
    {
        var properties = request.Properties;
        if (properties == null)
        {
            return Default();
        }

        return new EventHubNetworkRuleSetSubresourceProperties
        {
            DefaultAction = properties.DefaultAction ?? "Allow",
            PublicNetworkAccess = properties.PublicNetworkAccess ?? "Enabled",
            TrustedServiceAccessEnabled = properties.TrustedServiceAccessEnabled,
            VirtualNetworkRules = properties.VirtualNetworkRules?
                .Select(rule => new VirtualNetworkRule
                {
                    IgnoreMissingVnetServiceEndpoint = rule.IgnoreMissingVnetServiceEndpoint,
                    Subnet = rule.Subnet
                })
                .ToArray() ?? [],
            IpRules = properties.IpRules?
                .Select(rule => new IpRule
                {
                    IpMask = rule.IpMask,
                    Action = rule.Action
                })
                .ToArray() ?? []
        };
    }

    internal sealed class VirtualNetworkRule
    {
        public bool IgnoreMissingVnetServiceEndpoint { get; set; }
        public string? Subnet { get; set; }
    }

    internal sealed class IpRule
    {
        public string? IpMask { get; set; }
        public string Action { get; set; } = "Allow";
    }
}