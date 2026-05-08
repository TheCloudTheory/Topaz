using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Service.KeyVault.Models;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses.Certificates;

internal class GetDeletedCertificatesResponse
{
    [JsonPropertyName("value")]
    public DeletedCertItem[]? Value { get; init; }

    [JsonPropertyName("nextLink")]
    public string NextLink { get; init; } = string.Empty;

    public class DeletedCertItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("x5t")]
        public string? X5t { get; init; }

        [JsonPropertyName("recoveryId")]
        public string? RecoveryId { get; init; }

        [JsonPropertyName("deletedDate")]
        public long DeletedDate { get; init; }

        [JsonPropertyName("scheduledPurgeDate")]
        public long ScheduledPurgeDate { get; init; }

        [JsonPropertyName("attributes")]
        public DeletedCertItemAttributes? Attributes { get; init; }

        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; init; }

        public class DeletedCertItemAttributes
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

    public static GetDeletedCertificatesResponse FromRecords(
        IEnumerable<DeletedCertificateRecord> records, string vaultName)
    {
        var items = records.Select(record =>
        {
            var bundle = record.Bundles.Last();
            return new DeletedCertItem
            {
                Id = bundle.Id,
                X5t = bundle.X5t,
                Tags = bundle.Tags,
                DeletedDate = record.DeletedDate,
                ScheduledPurgeDate = record.ScheduledPurgeDate,
                RecoveryId = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/deletedcertificates/{record.CertName}",
                Attributes = bundle.Attributes == null ? null : new DeletedCertItem.DeletedCertItemAttributes
                {
                    Enabled = bundle.Attributes.Enabled,
                    Created = bundle.Attributes.Created,
                    Updated = bundle.Attributes.Updated
                }
            };
        }).ToArray();

        return new GetDeletedCertificatesResponse { Value = items };
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
