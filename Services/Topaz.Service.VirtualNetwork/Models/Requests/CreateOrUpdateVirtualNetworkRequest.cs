using System.Text.Json;
using JetBrains.Annotations;

namespace Topaz.Service.VirtualNetwork.Models.Requests;

public sealed class CreateOrUpdateVirtualNetworkRequest
{
    public CreateOrUpdateVirtualNetworkRequestProperties? Properties { get; init; }

    [UsedImplicitly]
    public class CreateOrUpdateVirtualNetworkRequestProperties
    {
        public Models.VirtualNetworkResourceProperties.VirtualNetworkAddressSpace? AddressSpace { get; set; }
        public int? FlowTimeoutInMinutes { get; set; }
        public JsonElement? Subnets { get; set; }
        public JsonElement? VirtualNetworkPeerings { get; set; }
        public Guid? ResourceGuid { get; set; }
        public bool? EnableDdosProtection { get; set; }
        public bool? EnableVmProtection { get; set; }
        public JsonElement? BgpCommunities { get; set; }
        public JsonElement? Encryption { get; set; }
        public JsonElement? IPAllocations { get; set; }
        public JsonElement? FlowLogs { get; set; }
        public string? PrivateEndpointVnetPolicy { get; set; }
        public JsonElement? DhcpOptions { get; set; }
    }
}
