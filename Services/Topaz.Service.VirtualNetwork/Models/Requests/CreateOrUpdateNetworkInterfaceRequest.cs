using System.Text.Json;

namespace Topaz.Service.VirtualNetwork.Models.Requests;

internal record CreateOrUpdateNetworkInterfaceRequest
{
    public string? Location { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
    public CreateOrUpdateNetworkInterfaceRequestProperties? Properties { get; init; }

    internal class CreateOrUpdateNetworkInterfaceRequestProperties
    {
        public JsonElement? IpConfigurations { get; init; }
        public JsonElement? NetworkSecurityGroup { get; init; }
        public bool? EnableAcceleratedNetworking { get; init; }
        public bool? EnableIPForwarding { get; init; }
    }
}
