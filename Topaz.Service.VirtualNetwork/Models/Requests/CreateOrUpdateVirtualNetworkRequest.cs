using Azure;
using Azure.Core;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources.Models;
using JetBrains.Annotations;

namespace Topaz.Service.VirtualNetwork.Models.Requests;

public sealed class CreateOrUpdateVirtualNetworkRequest
{
    public CreateOrUpdateVirtualNetworkRequestProperties? Properties { get; init; }
    
    public class CreateOrUpdateVirtualNetworkRequestProperties
    {
        public ExtendedLocation? ExtendedLocation { get; set; }
        public ETag? ETag { get; set; }
        public Models.VirtualNetworkResourceProperties.VirtualNetworkAddressSpace? AddressSpace { get; set; }
        public int? FlowTimeoutInMinutes { get; set; }
        public IList< Models.VirtualNetworkResourceProperties.SubnetData>? Subnets { get; set; }
        public IList<VirtualNetworkPeeringData>? VirtualNetworkPeerings { get; set; }
        public Guid? ResourceGuid { get; set; }
        public NetworkProvisioningState? ProvisioningState { get; set; }
        public bool? EnableDdosProtection { get; set; }
        public bool? EnableVmProtection { get; set; }
        public VirtualNetworkBgpCommunities? BgpCommunities { get; set; }
        public VirtualNetworkEncryption? Encryption { get; set; }
        public IList<WritableSubResource>? IPAllocations { get; }
        public IReadOnlyList<FlowLogData>? FlowLogs { get; set; }
        public PrivateEndpointVnetPolicy? PrivateEndpointVnetPolicy { get; set; }
    }
}