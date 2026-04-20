using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models;

internal record class KeyRotationPolicy
{
    public string? Id { get; init; }
    public KeyRotationPolicyAttributes? Attributes { get; init; }
    public KeyRotationLifetimeAction[]? LifetimeActions { get; init; }

    public static KeyRotationPolicy Default(string vaultName, string keyName) =>
        new()
        {
            Id = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/keys/{keyName}/rotationpolicy",
            Attributes = new KeyRotationPolicyAttributes(
                Created: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Updated: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ExpiryTime: null),
            LifetimeActions = []
        };

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

internal record class KeyRotationPolicyAttributes
{
    public KeyRotationPolicyAttributes() { }

    public KeyRotationPolicyAttributes(long Created, long Updated, string? ExpiryTime)
    {
        this.Created = Created;
        this.Updated = Updated;
        this.ExpiryTime = ExpiryTime;
    }

    public long Created { get; init; }
    public long Updated { get; init; }
    public string? ExpiryTime { get; init; }
}

internal record class KeyRotationLifetimeAction
{
    public KeyRotationLifetimeActionTrigger? Trigger { get; init; }
    public KeyRotationLifetimeActionType? Action { get; init; }
}

internal record class KeyRotationLifetimeActionTrigger
{
    public string? TimeAfterCreate { get; init; }
    public string? TimeBeforeExpiry { get; init; }
}

internal record class KeyRotationLifetimeActionType
{
    public string? Type { get; init; }
}
