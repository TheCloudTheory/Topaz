using System.Text.Json;

namespace Topaz.Service.VirtualNetwork.Models.Requests;

internal record CreateOrUpdateNetworkInterfaceRequest
{
    public string? Location { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
    public CreateOrUpdateNetworkInterfaceRequestProperties? Properties { get; init; }

    internal class CreateOrUpdateNetworkInterfaceRequestProperties
    {
        public List<PrivateEndpointIpConfiguration>? IpConfigurations { get; init; }
        public JsonElement? NetworkSecurityGroup { get; init; }
        public bool? EnableAcceleratedNetworking { get; init; }
        public bool? EnableIpForwarding { get; init; }
    }

    public static CreateOrUpdateNetworkInterfaceRequest ForPrivateEndpointUsingDynamicIp(string location, string subnetId)
    {
        return new CreateOrUpdateNetworkInterfaceRequest
        {
            Location = location,
            Properties = new CreateOrUpdateNetworkInterfaceRequestProperties
            {
                IpConfigurations =
                [
                        new PrivateEndpointIpConfiguration
                        {
                            Name = "privateEndpointIpConfig",
                            Properties = new PrivateEndpointIpConfigurationProperties
                            {
                                PrivateIPAllocationMethod = "Dynamic",
                                PrivateIPAddressVersion = "IPv4",
                                Subnet = new SubnetResource
                                {
                                    Id = subnetId
                                }
                            }
                        }
                ],
                EnableAcceleratedNetworking = false,
                EnableIpForwarding = false
            }
        };
    }
    
    public static CreateOrUpdateNetworkInterfaceRequest ForPrivateEndpointUsingStaticIPs(string[] ips, string location, string subnetId)
    {
        return new CreateOrUpdateNetworkInterfaceRequest
        {
            Location = location,
            Properties = new CreateOrUpdateNetworkInterfaceRequestProperties
            {
                IpConfigurations =
                [
                    .. ips.Select(ip =>
                        new PrivateEndpointIpConfiguration
                        {
                            Name = "privateEndpointIpConfig",
                            Properties = new PrivateEndpointIpConfigurationProperties
                            {
                                PrivateIPAddress = ip,
                                PrivateIPAllocationMethod = "Static",
                                PrivateIPAddressVersion = "IPv4",
                                Subnet = new SubnetResource
                                {
                                    Id = subnetId
                                }
                            }
                        }
                    )
                ],
                EnableAcceleratedNetworking = false,
                EnableIpForwarding = false
            }
        };
    }
}
