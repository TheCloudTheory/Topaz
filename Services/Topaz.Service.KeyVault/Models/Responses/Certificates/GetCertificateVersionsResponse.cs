using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Service.KeyVault.Models;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses.Certificates;

public class GetCertificateVersionsResponse
{
    [JsonPropertyName("value")]
    public CertificateVersionItem[]? Value { get; init; }

    [JsonPropertyName("nextLink")]
    public string NextLink { get; init; } = string.Empty;

    public class CertificateVersionItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("x5t")]
        public string? X5t { get; init; }

        [JsonPropertyName("attributes")]
        public CertificateVersionAttributes? Attributes { get; init; }

        public class CertificateVersionAttributes
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

    public static GetCertificateVersionsResponse FromBundles(CertificateBundle[] bundles) => new()
    {
        Value = bundles.Select(b => new CertificateVersionItem
        {
            Id = b.Id,
            X5t = b.X5t,
            Attributes = b.Attributes == null ? null : new CertificateVersionItem.CertificateVersionAttributes
            {
                Enabled = b.Attributes.Enabled,
                Created = b.Attributes.Created,
                Updated = b.Attributes.Updated
            }
        }).ToArray()
    };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
