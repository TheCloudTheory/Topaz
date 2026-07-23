using System.Text.Json;

namespace Topaz.Service.VirtualNetwork.Models.Requests;

internal record CreateOrUpdatePrivateEndpointRequest
{
    public string? Location { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
    public CreateOrUpdatePrivateEndpointRequestProperties? Properties { get; init; }

    internal class CreateOrUpdatePrivateEndpointRequestProperties
    {
        public JsonElement? Subnet { get; init; }
        public JsonElement? PrivateLinkServiceConnections { get; init; }
        public JsonElement? CustomDnsConfigs { get; init; }
    }
}
