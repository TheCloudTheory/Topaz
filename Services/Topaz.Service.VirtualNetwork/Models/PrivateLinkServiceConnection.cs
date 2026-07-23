namespace Topaz.Service.VirtualNetwork.Models;

public sealed class PrivateLinkServiceConnection
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Etag { get; set; }
    public string? Type { get; set; }
    public PrivateLinkServiceConnectionProperties? Properties { get; set; }
}
