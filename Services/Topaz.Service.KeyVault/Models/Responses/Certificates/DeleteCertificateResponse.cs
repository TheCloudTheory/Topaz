using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Service.KeyVault.Models;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses.Certificates;

public class DeleteCertificateResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("recoveryId")]
    public string? RecoveryId { get; init; }

    [JsonPropertyName("deletedDate")]
    public long DeletedDate { get; init; }

    [JsonPropertyName("scheduledPurgeDate")]
    public long ScheduledPurgeDate { get; init; }

    [JsonPropertyName("cer")]
    public string? Cer { get; init; }

    [JsonPropertyName("x5t")]
    public string? X5t { get; init; }

    [JsonPropertyName("kid")]
    public string? Kid { get; init; }

    [JsonPropertyName("sid")]
    public string? Sid { get; init; }

    [JsonPropertyName("attributes")]
    public CertificateAttributes? Attributes { get; init; }

    [JsonPropertyName("policy")]
    public CertificatePolicy? Policy { get; init; }

    public static DeleteCertificateResponse New(CertificateBundle bundle, string vaultName, string certName)
    {
        return new DeleteCertificateResponse
        {
            Id = bundle.Id,
            Cer = bundle.Cer,
            X5t = bundle.X5t,
            Kid = bundle.Kid,
            Sid = bundle.Sid,
            Attributes = bundle.Attributes,
            Policy = bundle.Policy,
            DeletedDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ScheduledPurgeDate = DateTimeOffset.UtcNow.AddDays(90).ToUnixTimeSeconds(),
            RecoveryId = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/deletedcertificates/{certName}"
        };
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
