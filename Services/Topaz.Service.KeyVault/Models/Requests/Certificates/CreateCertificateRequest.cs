using System.Text.Json.Serialization;
using Topaz.Service.KeyVault.Models;

namespace Topaz.Service.KeyVault.Models.Requests.Certificates;

public record CreateCertificateRequest
{
    [JsonPropertyName("policy")]
    public CertificatePolicy? Policy { get; init; }

    [JsonPropertyName("attributes")]
    public CertificateAttributes? Attributes { get; init; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; init; }
}
