using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Topaz.Service.VirtualNetwork.Models.Requests;

namespace Topaz.Service.VirtualNetwork.Models;

public sealed class NetworkSecurityGroupResourceProperties
{
    public JsonElement? SecurityRules { get; set; }

    [UsedImplicitly] public string ProvisioningState => "Succeeded";

    [UsedImplicitly] public IReadOnlyList<DefaultSecurityRule> DefaultSecurityRules { get; } = DefaultRules;

    private static readonly DefaultSecurityRule[] DefaultRules =
    [
        new()
        {
            Name = "AllowVnetInBound",
            Properties = new DefaultSecurityRuleProperties
            {
                Description = "Allow inbound traffic from all VMs in VNET",
                Protocol = "*",
                SourcePortRange = "*",
                DestinationPortRange = "*",
                SourceAddressPrefix = "VirtualNetwork",
                DestinationAddressPrefix = "VirtualNetwork",
                Access = "Allow",
                Priority = 65000,
                Direction = "Inbound"
            }
        },
        new()
        {
            Name = "AllowAzureLoadBalancerInBound",
            Properties = new DefaultSecurityRuleProperties
            {
                Description = "Allow inbound traffic from azure load balancer",
                Protocol = "*",
                SourcePortRange = "*",
                DestinationPortRange = "*",
                SourceAddressPrefix = "AzureLoadBalancer",
                DestinationAddressPrefix = "*",
                Access = "Allow",
                Priority = 65001,
                Direction = "Inbound"
            }
        },
        new()
        {
            Name = "DenyAllInBound",
            Properties = new DefaultSecurityRuleProperties
            {
                Description = "Deny all inbound traffic",
                Protocol = "*",
                SourcePortRange = "*",
                DestinationPortRange = "*",
                SourceAddressPrefix = "*",
                DestinationAddressPrefix = "*",
                Access = "Deny",
                Priority = 65500,
                Direction = "Inbound"
            }
        },
        new()
        {
            Name = "AllowVnetOutBound",
            Properties = new DefaultSecurityRuleProperties
            {
                Description = "Allow outbound traffic from all VMs to all VMs in VNET",
                Protocol = "*",
                SourcePortRange = "*",
                DestinationPortRange = "*",
                SourceAddressPrefix = "VirtualNetwork",
                DestinationAddressPrefix = "VirtualNetwork",
                Access = "Allow",
                Priority = 65000,
                Direction = "Outbound"
            }
        },
        new()
        {
            Name = "AllowInternetOutBound",
            Properties = new DefaultSecurityRuleProperties
            {
                Description = "Allow outbound traffic from all VMs to Internet",
                Protocol = "*",
                SourcePortRange = "*",
                DestinationPortRange = "*",
                SourceAddressPrefix = "*",
                DestinationAddressPrefix = "Internet",
                Access = "Allow",
                Priority = 65001,
                Direction = "Outbound"
            }
        },
        new()
        {
            Name = "DenyAllOutBound",
            Properties = new DefaultSecurityRuleProperties
            {
                Description = "Deny all outbound traffic",
                Protocol = "*",
                SourcePortRange = "*",
                DestinationPortRange = "*",
                SourceAddressPrefix = "*",
                DestinationAddressPrefix = "*",
                Access = "Deny",
                Priority = 65500,
                Direction = "Outbound"
            }
        }
    ];

    internal static NetworkSecurityGroupResourceProperties FromRequest(
        CreateOrUpdateNetworkSecurityGroupRequest request)
    {
        return new NetworkSecurityGroupResourceProperties
        {
            SecurityRules = request.Properties?.SecurityRules
        };
    }

    public sealed class DefaultSecurityRule
    {
        public required string Name { get; init; }
        public required DefaultSecurityRuleProperties Properties { get; init; }

        [JsonIgnore] [UsedImplicitly] public string Type => "Microsoft.Network/networkSecurityGroups/defaultSecurityRules";
    }

    public sealed class DefaultSecurityRuleProperties
    {
        public string? Description { get; init; }
        public required string Protocol { get; init; }
        public required string SourcePortRange { get; init; }
        public required string DestinationPortRange { get; init; }
        public required string SourceAddressPrefix { get; init; }
        public required string DestinationAddressPrefix { get; init; }
        public required string Access { get; init; }
        public required int Priority { get; init; }
        public required string Direction { get; init; }
        [UsedImplicitly] public string ProvisioningState => "Succeeded";
    }
}
