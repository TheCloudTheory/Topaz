namespace Azure.Local.Service.KeyVault.Models;

public record class Secret(string Name, string Value)
{
    public string Id => $"https://myvault.localhost/secrets/{Name}/{Guid.NewGuid}";

    public SecretAttributes Attributes => new(true, DateTimeOffset.Now.Ticks, DateTimeOffset.Now.Ticks);
}

public record class SecretAttributes(bool Enabled, long Created, long Updated)
{
    public string RecoveryLevel => "Recoverable+Purgeable";
}
