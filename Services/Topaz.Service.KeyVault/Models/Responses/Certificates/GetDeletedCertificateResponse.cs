using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Service.KeyVault.Models;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses.Certificates;

internal class GetDeletedCertificateResponse
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

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; init; }

    public static GetDeletedCertificateResponse FromRecord(DeletedCertificateRecord record, string vaultName)
    {
        var bundle = record.Bundles.Last();
        return new GetDeletedCertificateResponse
        {
            Id = bundle.Id,
            Cer = bundle.Cer,
            X5t = bundle.X5t,
            Kid = bundle.Kid,
            Sid = bundle.Sid,
            Attributes = bundle.Attributes,
            Policy = bundle.Policy,
            Tags = bundle.Tags,
            DeletedDate = record.DeletedDate,
            ScheduledPurgeDate = record.ScheduledPurgeDate,
            RecoveryId = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/deletedcertificates/{record.CertName}"
        };
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
