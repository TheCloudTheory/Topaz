namespace Topaz.Portal.Models.ContainerRegistry;

public sealed class ContainerRegistryDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Location { get; init; }
    public string? ResourceGroupName { get; init; }
    public string? SubscriptionId { get; init; }
    public string? SubscriptionName { get; init; }
    public string? LoginServer { get; init; }
    public string? SkuName { get; init; }
    public bool AdminUserEnabled { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ListContainerRegistriesResponse
{
    public ContainerRegistryDto[] Value { get; init; } = [];
}
