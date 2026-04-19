using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;
using Topaz.Service.KeyVault.Models.Requests;

namespace Topaz.Service.KeyVault.Models;

/// <summary>
/// Represents a stored key bundle — the outer envelope returned by the Key Vault data-plane.
/// Each persisted file holds an array of KeyBundle (one per version).
/// </summary>
public record class KeyBundle
{
    [System.Text.Json.Serialization.JsonConstructor]
    public KeyBundle() { }

    public KeyBundle(string name, string vaultName, string keyType, int? keySize,
        string? curve, string[] keyOperations, byte[]? rsaN, byte[]? rsaE,
        byte[]? ecX, byte[]? ecY)
    {
        var version = Guid.NewGuid();
        Name = name;

        Key = new JsonWebKey
        {
            Kid = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/keys/{name}/{version:N}",
            Kty = keyType,
            KeyOps = keyOperations,
            // RSA components
            N = rsaN != null ? Base64UrlEncode(rsaN) : null,
            E = rsaE != null ? Base64UrlEncode(rsaE) : null,
            // EC components
            Crv = curve,
            X = ecX != null ? Base64UrlEncode(ecX) : null,
            Y = ecY != null ? Base64UrlEncode(ecY) : null,
        };

        Attributes = new KeyAttributes(
            Enabled: true,
            Created: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Updated: DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    [JsonPropertyName("key")]
    public JsonWebKey Key { get; set; } = null!;

    [JsonPropertyName("attributes")]
    public KeyAttributes Attributes { get; set; } = null!;

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; set; }

    // Stored internally – not serialized in the REST response.
    [JsonIgnore]
    public string Name { get; set; } = string.Empty;

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    public void UpdateFromRequest(UpdateKeyRequest request)
    {
        Attributes = Attributes with
        {
            Enabled = request.Attributes?.Enabled ?? Attributes.Enabled,
            Updated = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        if (request.KeyOps != null)
            Key = Key with { KeyOps = request.KeyOps };

        if (request.Tags != null)
            Tags = request.Tags;
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

public record class JsonWebKey
{
    [JsonPropertyName("kid")]
    public string Kid { get; set; } = string.Empty;

    [JsonPropertyName("kty")]
    public string Kty { get; set; } = string.Empty;

    [JsonPropertyName("key_ops")]
    public string[] KeyOps { get; set; } = [];

    // RSA
    [JsonPropertyName("n")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? N { get; set; }

    [JsonPropertyName("e")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? E { get; set; }

    // EC
    [JsonPropertyName("crv")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Crv { get; set; }

    [JsonPropertyName("x")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? X { get; set; }

    [JsonPropertyName("y")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Y { get; set; }
}

public record class KeyAttributes
{
    public KeyAttributes() { }

    public KeyAttributes(bool Enabled, long Created, long Updated)
    {
        this.Enabled = Enabled;
        this.Created = Created;
        this.Updated = Updated;
    }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("updated")]
    public long Updated { get; set; }

    [JsonPropertyName("recoveryLevel")]
    public string RecoveryLevel => "Recoverable+Purgeable";
}
