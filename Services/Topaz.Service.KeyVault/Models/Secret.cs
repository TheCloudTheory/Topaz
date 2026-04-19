using System.Text.Json;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models;

public record class Secret
{
    [System.Text.Json.Serialization.JsonConstructor]
    public Secret() { }

    public Secret(string name, string value, Guid version, string vaultName = "")
    {
        Id = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/secrets/{name}/{version}";
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
    public string? ContentType { get; set; }

    public void UpdateFromRequest(UpdateSecretRequest request)
    {
        Attributes = Attributes with
        {
            Enabled = request.Attributes?.Enabled ?? Attributes.Enabled,
            Updated = DateTimeOffset.Now.ToUnixTimeSeconds()
        };

        if (request.ContentType != null)
        {
            ContentType = request.ContentType;
        }
    }
    
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}

public record SecretAttributes(bool Enabled, long Created, long Updated)
{
    public string RecoveryLevel => "Recoverable+Purgeable";
}
