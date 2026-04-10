using JetBrains.Annotations;

namespace Topaz.Service.EventHub.Models.Requests;

public sealed class CreateOrUpdateNamespaceNetworkRuleSetRequest
{
    public CreateOrUpdateNamespaceNetworkRuleSetRequestProperties? Properties { get; init; }

    [UsedImplicitly]
    public sealed class CreateOrUpdateNamespaceNetworkRuleSetRequestProperties
    {
        public string? DefaultAction { get; set; } = "Allow";
        public string? PublicNetworkAccess { get; set; } = "Enabled";
        public bool TrustedServiceAccessEnabled { get; set; }
        public IReadOnlyList<VirtualNetworkRule>? VirtualNetworkRules { get; set; }
        public IReadOnlyList<IpRule>? IpRules { get; set; }

        [UsedImplicitly]
        public sealed class VirtualNetworkRule
        {
            public bool IgnoreMissingVnetServiceEndpoint { get; set; }
            public string? Subnet { get; set; }
        }

        [UsedImplicitly]
        public sealed class IpRule
        {
            public string? IpMask { get; set; }
            public string Action { get; set; } = "Allow";
        }
    }
}