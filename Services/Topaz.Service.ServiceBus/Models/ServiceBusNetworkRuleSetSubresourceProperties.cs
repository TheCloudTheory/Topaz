namespace Topaz.Service.ServiceBus.Models;

internal sealed class ServiceBusNetworkRuleSetSubresourceProperties
{
    public string DefaultAction { get; set; } = "Allow";
    public string PublicNetworkAccess { get; set; } = "Enabled";
    public bool TrustedServiceAccessEnabled { get; set; }
    public IReadOnlyList<VirtualNetworkRule> VirtualNetworkRules { get; set; } = [];
    public IReadOnlyList<IpRule> IpRules { get; set; } = [];

    public static ServiceBusNetworkRuleSetSubresourceProperties Default()
    {
        return new ServiceBusNetworkRuleSetSubresourceProperties();
    }

    internal sealed class VirtualNetworkRule
    {
        public bool IgnoreMissingVnetServiceEndpoint { get; set; }
        public SubnetReference? Subnet { get; set; }
    }

    internal sealed class SubnetReference
    {
        public string? Id { get; set; }
    }

    internal sealed class IpRule
    {
        public string? IpMask { get; set; }
        public string? Action { get; set; }
    }
}
