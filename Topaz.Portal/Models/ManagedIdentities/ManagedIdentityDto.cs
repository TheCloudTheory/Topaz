namespace Topaz.Portal.Models.ManagedIdentities;

public sealed class ManagedIdentityDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Location { get; init; }
    public string? ResourceGroupName { get; init; }
    public string? SubscriptionId { get; init; }
    public string? SubscriptionName { get; init; }
    public string? ClientId { get; init; }
    public string? PrincipalId { get; init; }
    public string? TenantId { get; init; }
    public string? ProvisioningState { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ListManagedIdentitiesResponse
{
    public ManagedIdentityDto[] Value { get; init; } = [];
}
