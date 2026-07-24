namespace Topaz.Service.VirtualNetwork.Models;

public sealed class PrivateEndpointIpConfiguration
{
    public string? Name { get; set; }
    public string? Etag { get; set; }
    public string? Type { get; set; }
    public PrivateEndpointIpConfigurationProperties? Properties { get; set; }
}

public sealed class PrivateEndpointIpConfigurationProperties
{
    public string? GroupId { get; set; }
    public string? MemberName { get; set; }
    public string? PrivateIPAddress { get; set; }
    public string? PrivateIPAllocationMethod { get; set; }
    public string? PrivateIPAddressVersion { get; set; }
    public SubnetResource? Subnet { get; set; }
}
