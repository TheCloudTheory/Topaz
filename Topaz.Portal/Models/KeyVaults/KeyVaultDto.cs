namespace Topaz.Portal.Models.KeyVaults;

public sealed class KeyVaultDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Location { get; init; }
    public string? ResourceGroupName { get; init; }
    public string? SubscriptionId { get; init; }
    public string? SubscriptionName { get; init; }
    public string? VaultUri { get; init; }
    public string? SkuName { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ListKeyVaultsResponse
{
    public KeyVaultDto[] Value { get; init; } = [];
}

public sealed class KeyVaultSecretDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? ContentType { get; init; }
    public bool Enabled { get; init; }
    public DateTimeOffset? Created { get; init; }
    public DateTimeOffset? Updated { get; init; }
}

public sealed class KeyVaultDeletedSecretDto
{
    public string? Name { get; init; }
    public string? ContentType { get; init; }
    public bool Enabled { get; init; }
    public DateTimeOffset? DeletedDate { get; init; }
    public DateTimeOffset? ScheduledPurgeDate { get; init; }
}

