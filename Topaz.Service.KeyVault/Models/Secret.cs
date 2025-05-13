namespace Topaz.Service.KeyVault.Models;

public record class Secret(string Name, string Value)
{
    public string Id => $"https://myvault.localhost/secrets/{Name}/{Guid.NewGuid()}";

    public SecretAttributes Attributes => new(true, DateTimeOffset.Now.ToUnixTimeSeconds(), DateTimeOffset.Now.ToUnixTimeSeconds());
}

public record class SecretAttributes(bool Enabled, long Created, long Updated)
{
    public string RecoveryLevel => "Recoverable+Purgeable";
}
