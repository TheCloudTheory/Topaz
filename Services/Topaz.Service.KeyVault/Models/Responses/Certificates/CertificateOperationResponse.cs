using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses.Certificates;

/// <summary>
/// Represents the pending certificate operation returned from
/// GET /certificates/{name}/pending and POST /certificates/{name}/create.
/// </summary>
public class CertificateOperationResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>"completed", "inProgress", or "failed".</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = "completed";

    [JsonPropertyName("statusDetails")]
    public string? StatusDetails { get; init; }

    [JsonPropertyName("csr")]
    public string? Csr { get; init; }

    [JsonPropertyName("cancellation_requested")]
    public bool? CancellationRequested { get; init; }

    [JsonPropertyName("target")]
    public string? Target { get; init; }

    [JsonPropertyName("issuer")]
    public OperationIssuerParameters? Issuer { get; init; }

    public class OperationIssuerParameters
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = "Self";
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
