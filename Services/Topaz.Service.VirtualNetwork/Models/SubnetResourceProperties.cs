using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Service.VirtualNetwork.Models.Requests;

namespace Topaz.Service.VirtualNetwork.Models;

public sealed class SubnetResourceProperties
{
    public string? AddressPrefix { get; set; }
    public IList<string>? AddressPrefixes { get; set; }
    public string ProvisioningState => "Succeeded";
    public JsonElement? ServiceEndpoints { get; set; }
    public JsonElement? Delegations { get; set; }
    public string? PrivateEndpointNetworkPolicies { get; set; }
    public string? PrivateLinkServiceNetworkPolicies { get; set; }
    public JsonElement? NetworkSecurityGroup { get; set; }
    public JsonElement? RouteTable { get; set; }

    [UsedImplicitly]
    public static SubnetResourceProperties FromRequest(CreateOrUpdateSubnetRequest request)
    {
        return new SubnetResourceProperties
        {
            AddressPrefix = request.Properties?.AddressPrefix,
            AddressPrefixes = request.Properties?.AddressPrefixes,
            ServiceEndpoints = request.Properties?.ServiceEndpoints,
            Delegations = request.Properties?.Delegations,
            PrivateEndpointNetworkPolicies = request.Properties?.PrivateEndpointNetworkPolicies,
            PrivateLinkServiceNetworkPolicies = request.Properties?.PrivateLinkServiceNetworkPolicies,
            NetworkSecurityGroup = request.Properties?.NetworkSecurityGroup,
            RouteTable = request.Properties?.RouteTable
        };
    }
}
