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
        byte[]? ecX, byte[]? ecY,
        byte[]? rsaD = null, byte[]? rsaP = null, byte[]? rsaQ = null,
        byte[]? rsaDP = null, byte[]? rsaDQ = null, byte[]? rsaInverseQ = null,
        byte[]? ecD = null)
    {
        var version = Guid.NewGuid();
        Name = name;

        Key = new JsonWebKey
        {
            Kid = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/keys/{name}/{version:N}",
            Kty = keyType,
            KeyOps = keyOperations,
            // RSA public components
            N = rsaN != null ? Base64UrlEncode(rsaN) : null,
            E = rsaE != null ? Base64UrlEncode(rsaE) : null,
            // RSA private components (D is also used for EC private scalar)
            D         = rsaD        != null ? Base64UrlEncode(rsaD)        :
                        ecD         != null ? Base64UrlEncode(ecD)         : null,
            P         = rsaP        != null ? Base64UrlEncode(rsaP)        : null,
            Q         = rsaQ        != null ? Base64UrlEncode(rsaQ)        : null,
            DP        = rsaDP       != null ? Base64UrlEncode(rsaDP)       : null,
            DQ        = rsaDQ       != null ? Base64UrlEncode(rsaDQ)       : null,
            InverseQ  = rsaInverseQ != null ? Base64UrlEncode(rsaInverseQ) : null,
            // EC components
            Crv = curve,
            X = ecX != null ? Base64UrlEncode(ecX) : null,
            Y = ecY != null ? Base64UrlEncode(ecY) : null,
        };

        Attributes = new KeyAttributes(
            Enabled: true,
            Created: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Updated: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Exportable: false);
    }

    [JsonPropertyName("key")]
    public JsonWebKey Key { get; set; } = null!;

    [JsonPropertyName("attributes")]
    public KeyAttributes Attributes { get; set; } = null!;

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; set; }

    public KeyReleasePolicy? ReleasePolicy { get; set; }

    // Stored internally – not serialized in the REST response.
    [JsonIgnore]
    public string Name { get; set; } = string.Empty;

    public override string ToString()
    {
        // Strip private key material so HTTP responses never leak it.
        var publicBundle = this with { Key = Key.ToPublicJwk() };
        return JsonSerializer.Serialize(publicBundle, GlobalSettings.JsonOptions);
    }

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

    // RSA private components — stored on disk, never sent in HTTP responses.
    [JsonPropertyName("d")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? D { get; set; }

    [JsonPropertyName("p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? P { get; set; }

    [JsonPropertyName("q")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Q { get; set; }

    [JsonPropertyName("dp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DP { get; set; }

    [JsonPropertyName("dq")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DQ { get; set; }

    [JsonPropertyName("qi")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InverseQ { get; set; }

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

    /// <summary>Returns a copy of this JWK with all private RSA fields removed — safe to include in API responses.</summary>
    public JsonWebKey ToPublicJwk() =>
        this with { D = null, P = null, Q = null, DP = null, DQ = null, InverseQ = null };
}

public record class KeyAttributes
{
    public KeyAttributes() { }

    public KeyAttributes(bool Enabled, long Created, long Updated, bool Exportable = false)
    {
        this.Enabled = Enabled;
        this.Created = Created;
        this.Updated = Updated;
        this.Exportable = Exportable;
    }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("updated")]
    public long Updated { get; set; }

    [JsonPropertyName("recoveryLevel")]
    public string RecoveryLevel => "Recoverable+Purgeable";

    public bool Exportable { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public KeyAttestation? Attestation { get; init; }
}

public record class KeyAttestation
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CertificatePemFile { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrivateKeyAttestation { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PublicKeyAttestation { get; init; }
}

public record class KeyReleasePolicy
{
    public string? ContentType { get; init; }
    public string? Data { get; init; }
}
