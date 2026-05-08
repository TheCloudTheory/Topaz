using System.Text.Json.Serialization;

namespace Topaz.Service.KeyVault.Models.Requests.Certificates;

public record UpdateCertificateRequest
{
    [JsonPropertyName("attributes")]
    public UpdateCertificateAttributes? Attributes { get; init; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; init; }

    public record UpdateCertificateAttributes
    {
        [JsonPropertyName("enabled")]
        public bool? Enabled { get; init; }
    }
}
