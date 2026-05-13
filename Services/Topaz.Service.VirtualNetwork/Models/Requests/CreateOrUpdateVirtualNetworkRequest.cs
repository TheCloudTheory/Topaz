using System.Text.Json;
using JetBrains.Annotations;

namespace Topaz.Service.VirtualNetwork.Models.Requests;

public sealed class CreateOrUpdateVirtualNetworkRequest
{
    public CreateOrUpdateVirtualNetworkRequestProperties? Properties { get; init; }

    public sealed class InlineSubnetEntry
    {
        public string? Name { get; set; }
        public string? Id { get; set; }
        public CreateOrUpdateSubnetRequest.CreateOrUpdateSubnetRequestProperties? Properties { get; set; }
    }

    [UsedImplicitly]
    public class CreateOrUpdateVirtualNetworkRequestProperties
    {
        public Models.VirtualNetworkResourceProperties.VirtualNetworkAddressSpace? AddressSpace { get; set; }
        public int? FlowTimeoutInMinutes { get; set; }
        public IList<InlineSubnetEntry>? Subnets { get; set; }
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
