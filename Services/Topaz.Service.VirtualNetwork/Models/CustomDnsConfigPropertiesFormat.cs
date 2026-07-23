namespace Topaz.Service.VirtualNetwork.Models;

public sealed class CustomDnsConfigPropertiesFormat
{
    public string? Fqdn { get; set; }
    public List<string>? IpAddresses { get; set; }
}
