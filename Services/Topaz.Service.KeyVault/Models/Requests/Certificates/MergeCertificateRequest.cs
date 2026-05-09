using System.Text.Json.Serialization;

namespace Topaz.Service.KeyVault.Models.Requests.Certificates;

/// <summary>
/// Request body for POST /certificates/{name}/pending/merge.
/// x5c is an ordered array of base64-encoded DER certificates:
/// index 0 is the signed leaf, followed by any intermediate/root certs.
/// </summary>
public record MergeCertificateRequest
{
    [JsonPropertyName("x5c")]
    public string[] X5c { get; init; } = [];

    [JsonPropertyName("attributes")]
    public CertificateAttributes? Attributes { get; init; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; init; }
}
