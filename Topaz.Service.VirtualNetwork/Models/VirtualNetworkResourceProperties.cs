using Azure;
using Azure.Core;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources.Models;
using JetBrains.Annotations;
using Topaz.Service.VirtualNetwork.Models.Requests;

namespace Topaz.Service.VirtualNetwork.Models;

public sealed class VirtualNetworkResourceProperties
{
    public VirtualNetworkAddressSpace? AddressSpace { get; set; }
    public int? FlowTimeoutInMinutes { get; set; }
    public IList<SubnetData>? Subnets { get; set; }
    public IList<VirtualNetworkPeeringData>? VirtualNetworkPeerings { get; set; }
    public Guid? ResourceGuid { get; set; }
    public string ProvisioningState => NetworkProvisioningState.Succeeded.ToString();
    public bool? EnableDdosProtection { get; set; }
    public bool? EnableVmProtection { get; set; }
    public VirtualNetworkBgpCommunities? BgpCommunities { get; set; }
    public VirtualNetworkEncryption? Encryption { get; set; }
    public IList<WritableSubResource>? IPAllocations { get; set; }
    public IReadOnlyList<FlowLogData>? FlowLogs { get; set; }
    public PrivateEndpointVnetPolicy? PrivateEndpointVnetPolicy { get; set; }

    public static VirtualNetworkResourceProperties FromRequest(CreateOrUpdateVirtualNetworkRequest request)
    {
        return new VirtualNetworkResourceProperties
        {
            AddressSpace = request.Properties?.AddressSpace,
            Subnets = request.Properties?.Subnets
        };
    }

    [UsedImplicitly]
    public class VirtualNetworkAddressSpace
    {
        public IList<string>? AddressPrefixes { get; set; }
        public IList<IpamPoolPrefixAllocation>? IpamPoolPrefixAllocations { get; set; }
    }

    [UsedImplicitly]
    public class SubnetData
    {
        public ResourceIdentifier? Id { get; set; }
        public string? Name { get; set; }
        public ResourceType? ResourceType { get; set; }
        public ETag? ETag { get; set; }
        public SubnetDataProperties? Properties { get; set; }

        public class SubnetDataProperties
        {
            public string? AddressPrefix { get; set; }
            public IList<string>? AddressPrefixes { get; set; }
            public NetworkSecurityGroupData? NetworkSecurityGroup { get; set; }
            public RouteTableData? RouteTable { get; set; }
            public ResourceIdentifier? NatGatewayId { get; set; }
            public IList<ServiceEndpointProperties>? ServiceEndpoints { get; set; }
            public IList<ServiceEndpointPolicyData>? ServiceEndpointPolicies { get; set; }
            public IReadOnlyList<PrivateEndpointData>? PrivateEndpoints { get; set; }
            public IReadOnlyList<NetworkIPConfiguration>? IPConfigurations { get; set; }
            public IReadOnlyList<NetworkIPConfigurationProfile>? IPConfigurationProfiles { get; set; }
            public IList<WritableSubResource>? IPAllocations { get; set; }
            public IReadOnlyList<ResourceNavigationLink>? ResourceNavigationLinks { get; set; }
            public IReadOnlyList<ServiceAssociationLink>? ServiceAssociationLinks { get; set; }
            public IList<ServiceDelegation>? Delegations { get; set; }
            public string? Purpose { get; set; }
            public NetworkProvisioningState? ProvisioningState { get; set; }
            public VirtualNetworkPrivateEndpointNetworkPolicy? PrivateEndpointNetworkPolicy { get; set; }
            public VirtualNetworkPrivateLinkServiceNetworkPolicy? PrivateLinkServiceNetworkPolicy { get; set; }
            public IList<ApplicationGatewayIPConfiguration>? ApplicationGatewayIPConfigurations { get; set; }
            public SharingScope? SharingScope { get; set; }
            public bool? DefaultOutboundAccess { get; set; }
            public IList<IpamPoolPrefixAllocation>? IpamPoolPrefixAllocations { get; set; }
        }
    }
}