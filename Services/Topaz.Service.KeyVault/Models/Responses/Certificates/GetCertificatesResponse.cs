using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Service.KeyVault.Models;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses.Certificates;

public class GetCertificatesResponse
{
    [JsonPropertyName("value")]
    public CertificateItem[]? Value { get; init; }

    [JsonPropertyName("nextLink")]
    public string NextLink { get; init; } = string.Empty;

    public class CertificateItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("x5t")]
        public string? X5t { get; init; }

        [JsonPropertyName("attributes")]
        public CertificateItemAttributes? Attributes { get; init; }

        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; init; }

        public class CertificateItemAttributes
        {
            [JsonPropertyName("enabled")]
            public bool Enabled { get; init; }

            [JsonPropertyName("created")]
            public long Created { get; init; }

            [JsonPropertyName("updated")]
            public long Updated { get; init; }

            [JsonPropertyName("recoveryLevel")]
            public string RecoveryLevel { get; init; } = "Recoverable+Purgeable";
        }
    }

    public static GetCertificatesResponse FromBundles(CertificateBundle[] bundles) => new()
    {
        Value = bundles.Select(b => new CertificateItem
        {
            Id = b.Id,
            X5t = b.X5t,
            Attributes = b.Attributes == null ? null : new CertificateItem.CertificateItemAttributes
            {
                Enabled = b.Attributes.Enabled,
                Created = b.Attributes.Created,
                Updated = b.Attributes.Updated
            },
            Tags = b.Tags
        }).ToArray()
    };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
