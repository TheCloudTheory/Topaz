using System.Text.Json;
using Topaz.Service.VirtualNetwork.Models.Requests;

namespace Topaz.Service.VirtualNetwork.Models;

public sealed class NetworkInterfaceResourceProperties
{
    public JsonElement? IpConfigurations { get; set; }
    public JsonElement? NetworkSecurityGroup { get; set; }
    public bool? EnableAcceleratedNetworking { get; set; }
    public bool? EnableIPForwarding { get; set; }
    public string ProvisioningState => "Succeeded";

    internal static NetworkInterfaceResourceProperties FromRequest(CreateOrUpdateNetworkInterfaceRequest request)
    {
        return new NetworkInterfaceResourceProperties
        {
            IpConfigurations = request.Properties?.IpConfigurations,
            NetworkSecurityGroup = request.Properties?.NetworkSecurityGroup,
            EnableAcceleratedNetworking = request.Properties?.EnableAcceleratedNetworking,
            EnableIPForwarding = request.Properties?.EnableIPForwarding
        };
    }

    internal static void UpdateFromRequest(
        NetworkInterfaceResourceProperties properties,
        CreateOrUpdateNetworkInterfaceRequest request)
    {
        properties.IpConfigurations = request.Properties?.IpConfigurations ?? properties.IpConfigurations;
        properties.NetworkSecurityGroup = request.Properties?.NetworkSecurityGroup ?? properties.NetworkSecurityGroup;
        properties.EnableAcceleratedNetworking = request.Properties?.EnableAcceleratedNetworking ?? properties.EnableAcceleratedNetworking;
        properties.EnableIPForwarding = request.Properties?.EnableIPForwarding ?? properties.EnableIPForwarding;
    }
}
