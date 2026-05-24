namespace Topaz.Service.VirtualNetwork.Models;

internal sealed class NicIpConfiguration
{
    public string? Name { get; set; }

    public NicIpConfigurationProperties? Properties { get; set; }
}

internal sealed class NicIpConfigurationProperties
{
    public NicIpSubnetReference? Subnet { get; set; }

    public string? PrivateIPAddress { get; set; }

    public string? PrivateIPAllocationMethod { get; set; }
}

internal sealed class NicIpSubnetReference
{
    public string? Id { get; set; }
}
