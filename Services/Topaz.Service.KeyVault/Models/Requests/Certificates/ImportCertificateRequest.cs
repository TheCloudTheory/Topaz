using System.Text.Json.Serialization;
using Topaz.Service.KeyVault.Models;

namespace Topaz.Service.KeyVault.Models.Requests.Certificates;

public record ImportCertificateRequest
{
    /// <summary>Base64-encoded PFX (PKCS#12) bytes of the certificate to import.</summary>
    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;

    /// <summary>Password for the PFX, if protected.</summary>
    [JsonPropertyName("pwd")]
    public string? Password { get; init; }

    [JsonPropertyName("policy")]
    public CertificatePolicy? Policy { get; init; }

    [JsonPropertyName("attributes")]
    public CertificateAttributes? Attributes { get; init; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; init; }
}
