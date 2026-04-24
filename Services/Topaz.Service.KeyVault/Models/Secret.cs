using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models;

public record Secret
{
    [JsonConstructor]
    public Secret()
    {
    }

    public Secret(string name, string value, Guid version, string vaultName = "")
    {
        Id = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/secrets/{name}/{version}";
        Name = name;
        Value = value;
        Version = version;
        Attributes = new SecretAttributes(true, DateTimeOffset.Now.ToUnixTimeSeconds(),
            DateTimeOffset.Now.ToUnixTimeSeconds());
    }

    public required string Id { get; init; }
    public required string Value { get; init; }
    public required string Name { get; init; }
    public Guid Version { get; set; }
    public required SecretAttributes Attributes { get; set; }
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
    [UsedImplicitly] public string RecoveryLevel => "Recoverable+Purgeable";
}