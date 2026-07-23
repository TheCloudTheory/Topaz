using JetBrains.Annotations;

namespace Topaz.Service.VirtualNetwork.Models;

public sealed class PrivateEndpointResourceProperties
{
    public SubnetResource? Subnet { get; set; }
    public List<PrivateLinkServiceConnection>? PrivateLinkServiceConnections { get; set; }
    public List<PrivateLinkServiceConnection>? ManualPrivateLinkServiceConnections { get; set; }
    public List<NetworkInterfaceResource>? NetworkInterfaces { get; set; }
    public List<CustomDnsConfigPropertiesFormat>? CustomDnsConfigs { get; set; }
    public List<PrivateEndpointIpConfiguration>? IpConfigurations { get; set; }
    public string? CustomNetworkInterfaceName { get; set; }
    [UsedImplicitly] public string ProvisioningState => "Succeeded";
}
