namespace Topaz.Portal.Models.PublicIps;

public sealed class PublicIpAddressDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Location { get; init; }
    public string? ResourceGroupName { get; init; }
    public string? SubscriptionId { get; init; }
    public string? SubscriptionName { get; init; }
    public string? IpAddress { get; init; }
    public string? AllocationMethod { get; init; }
    public string? IpVersion { get; init; }
    public string? Sku { get; init; }
    public int? IdleTimeoutInMinutes { get; init; }
    public string? ProvisioningState { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ListPublicIpAddressesResponse
{
    public PublicIpAddressDto[] Value { get; init; } = [];
}
