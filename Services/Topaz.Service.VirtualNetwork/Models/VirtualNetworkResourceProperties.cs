using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Service.VirtualNetwork.Models.Requests;

namespace Topaz.Service.VirtualNetwork.Models;

public sealed class VirtualNetworkResourceProperties
{
    public VirtualNetworkAddressSpace? AddressSpace { get; set; }
    public int? FlowTimeoutInMinutes { get; set; }
    public JsonElement? Subnets { get; set; }
    public JsonElement? VirtualNetworkPeerings { get; set; }
    public Guid? ResourceGuid { get; set; }
    public string ProvisioningState => "Succeeded";
    public bool? EnableDdosProtection { get; set; }
    public bool? EnableVmProtection { get; set; }
    public JsonElement? BgpCommunities { get; set; }
    public JsonElement? Encryption { get; set; }
    public JsonElement? IPAllocations { get; set; }
    public JsonElement? FlowLogs { get; set; }
    public string? PrivateEndpointVnetPolicy { get; set; }
    public JsonElement? DhcpOptions { get; set; }

    public static VirtualNetworkResourceProperties FromRequest(CreateOrUpdateVirtualNetworkRequest request)
    {
        return new VirtualNetworkResourceProperties
        {
            AddressSpace = request.Properties?.AddressSpace,
            Subnets = request.Properties?.Subnets,
            DhcpOptions = request.Properties?.DhcpOptions
        };
    }

    [UsedImplicitly]
    public class VirtualNetworkAddressSpace
    {
        public IList<string>? AddressPrefixes { get; set; }
        public JsonElement? IpamPoolPrefixAllocations { get; set; }
    }
}
