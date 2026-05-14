using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Service.VirtualNetwork.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork.Models;

public sealed class NetworkSecurityGroupResourceProperties
{
    public JsonElement? SecurityRules { get; set; }

    public string ProvisioningState => "Succeeded";

    public IReadOnlyList<DefaultSecurityRule> DefaultSecurityRules { get; } = _defaultRules;

    private static readonly DefaultSecurityRule[] _defaultRules =
    [
        new DefaultSecurityRule
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
        new DefaultSecurityRule
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
        new DefaultSecurityRule
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
        new DefaultSecurityRule
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
        new DefaultSecurityRule
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
        new DefaultSecurityRule
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

        [JsonIgnore]
        public string Type => "Microsoft.Network/networkSecurityGroups/defaultSecurityRules";
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
        public string ProvisioningState => "Succeeded";
    }
}
