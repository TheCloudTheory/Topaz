namespace Topaz.Service.VirtualNetwork.Models;

public sealed class PrivateLinkServiceConnectionProperties
{
    public string? PrivateLinkServiceId { get; set; }
    public List<string>? GroupIds { get; set; }
    public string? RequestMessage { get; set; }
    public string? ProvisioningState { get; set; }
    public PrivateLinkServiceConnectionState? PrivateLinkServiceConnectionState { get; set; }
}
