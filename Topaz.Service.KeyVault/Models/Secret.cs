using System.Text.Json.Serialization;

namespace Topaz.Service.KeyVault.Models;

public record class Secret
{
    public Secret(string name, string value, Guid version)
    {
        Id = $"https://myvault.localhost/secrets/{name}/{version}";
        Name = name;
        Value = value;
        Version = version;
        Attributes = new SecretAttributes(true, DateTimeOffset.Now.ToUnixTimeSeconds(), DateTimeOffset.Now.ToUnixTimeSeconds());
    }

    public string Id { get; set; }
    public string Value { get; set; }
    public string Name { get; set; }
    public Guid Version { get; set; }
    public SecretAttributes Attributes { get; set; }
}

public record class SecretAttributes(bool Enabled, long Created, long Updated)
{
    public string RecoveryLevel => "Recoverable+Purgeable";
}