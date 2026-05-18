namespace Topaz.Portal.Models.VirtualNetworks;

public sealed class VirtualNetworkDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Location { get; init; }
    public string? ResourceGroupName { get; init; }
    public string? SubscriptionId { get; init; }
    public string? SubscriptionName { get; init; }
    public IList<string>? AddressPrefixes { get; init; }
    public string? ProvisioningState { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ListVirtualNetworksResponse
{
    public VirtualNetworkDto[] Value { get; init; } = [];
}
