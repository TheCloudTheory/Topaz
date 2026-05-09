using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses.Certificates;

public class CertificateIssuersResponse
{
    [JsonPropertyName("value")]
    public IssuerItem[]? Value { get; init; }

    [JsonPropertyName("nextLink")]
    public string? NextLink { get; init; }

    public class IssuerItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("provider")]
        public string? Provider { get; init; }
    }

    public static CertificateIssuersResponse ForVault(string vaultName, IEnumerable<CertificateIssuerResponse> issuers)
    {
        var items = issuers.Select(i => new IssuerItem
        {
            Id = i.Id,
            Provider = i.Provider
        }).ToArray();

        return new CertificateIssuersResponse { Value = items, NextLink = null };
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
