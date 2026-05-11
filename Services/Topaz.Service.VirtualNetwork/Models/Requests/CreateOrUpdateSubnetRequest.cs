using System.Text.Json;
using JetBrains.Annotations;

namespace Topaz.Service.VirtualNetwork.Models.Requests;

public sealed class CreateOrUpdateSubnetRequest
{
    public CreateOrUpdateSubnetRequestProperties? Properties { get; init; }

    [UsedImplicitly]
    public sealed class CreateOrUpdateSubnetRequestProperties
    {
        public string? AddressPrefix { get; set; }
        public IList<string>? AddressPrefixes { get; set; }
        public JsonElement? ServiceEndpoints { get; set; }
        public JsonElement? Delegations { get; set; }
        public string? PrivateEndpointNetworkPolicies { get; set; }
        public string? PrivateLinkServiceNetworkPolicies { get; set; }
        public JsonElement? NetworkSecurityGroup { get; set; }
        public JsonElement? RouteTable { get; set; }
    }
}
