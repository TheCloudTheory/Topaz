namespace Topaz.Portal.Models.Storage;

public sealed class StorageAccountDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Location { get; init; }
    public string? ResourceGroupName { get; init; }
    public string? SubscriptionId { get; init; }
    public string? SubscriptionName { get; init; }
    public string? Kind { get; init; }
    public string? SkuName { get; init; }
    public string? BlobEndpoint { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ListStorageAccountsResponse
{
    public StorageAccountDto[] Value { get; init; } = [];
}
