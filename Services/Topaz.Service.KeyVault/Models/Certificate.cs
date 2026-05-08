using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models;

/// <summary>
/// Represents a stored certificate bundle — the outer envelope returned by the Key Vault data-plane.
/// Each persisted file holds an array of CertificateBundle (one per version).
/// </summary>
public record CertificateBundle
{
    [JsonConstructor]
    public CertificateBundle() { }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Base64-encoded DER representation of the certificate.</summary>
    [JsonPropertyName("cer")]
    public string? Cer { get; set; }

    /// <summary>SHA-1 thumbprint of the certificate, base64url-encoded.</summary>
    [JsonPropertyName("x5t")]
    public string? X5t { get; set; }

    /// <summary>URL of the associated key in the vault's key store (synthetic reference).</summary>
    [JsonPropertyName("kid")]
    public string? Kid { get; set; }

    /// <summary>URL of the associated secret in the vault's secret store (synthetic reference).</summary>
    [JsonPropertyName("sid")]
    public string? Sid { get; set; }

    [JsonPropertyName("attributes")]
    public CertificateAttributes? Attributes { get; set; }

    [JsonPropertyName("policy")]
    public CertificatePolicy? Policy { get; set; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; set; }

    /// <summary>Stored internally — not serialised in REST responses.</summary>
    [JsonIgnore]
    public string Name { get; set; } = string.Empty;

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    public void UpdateFromRequest(Requests.Certificates.UpdateCertificateRequest request)
    {
        if (request.Attributes != null && Attributes != null)
        {
            Attributes = Attributes with
            {
                Enabled = request.Attributes.Enabled ?? Attributes.Enabled,
                Updated = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        if (request.Tags != null)
            Tags = request.Tags;
    }
}

public record CertificateAttributes
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("nbf")]
    public long? NotBefore { get; set; }

    [JsonPropertyName("exp")]
    public long? Expires { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("updated")]
    public long Updated { get; set; }

    [JsonPropertyName("recoveryLevel")]
    public string RecoveryLevel { get; set; } = "Recoverable+Purgeable";
}
