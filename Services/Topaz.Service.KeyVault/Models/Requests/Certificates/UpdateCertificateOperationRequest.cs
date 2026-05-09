using System.Text.Json.Serialization;

namespace Topaz.Service.KeyVault.Models.Requests.Certificates;

public record UpdateCertificateOperationRequest
{
    [JsonPropertyName("cancellation_requested")]
    public bool CancellationRequested { get; init; }
}
