namespace Topaz.Service.VirtualNetwork.Models.Requests;

internal record CreateOrUpdatePrivateEndpointRequest
{
    public string? Location { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
    public CreateOrUpdatePrivateEndpointRequestProperties? Properties { get; init; }

    internal class CreateOrUpdatePrivateEndpointRequestProperties
    {
        public SubnetResource? Subnet { get; init; }
        public List<PrivateLinkServiceConnection>? PrivateLinkServiceConnections { get; init; }
        public List<PrivateLinkServiceConnection>? ManualPrivateLinkServiceConnections { get; init; }
        public List<CustomDnsConfigPropertiesFormat>? CustomDnsConfigs { get; init; }
        public List<PrivateEndpointIpConfiguration>? IpConfigurations { get; init; }
        public string? CustomNetworkInterfaceName { get; init; }
        public List<NetworkInterfaceResource>? NetworkInterfaces { get; set; }
    }
}
