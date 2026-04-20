using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

internal sealed class GetDeletedKeysResponse
{
    public DeletedKeyItem[]? Value { get; init; }
    public string NextLink { get; init; } = string.Empty;

    public class DeletedKeyItem
    {
        public string? RecoveryId { get; init; }
        public long DeletedDate { get; init; }
        public long ScheduledPurgeDate { get; init; }
        public string? Kid { get; init; }
        public DeletedKeyItemAttributes? Attributes { get; init; }

        public class DeletedKeyItemAttributes
        {
            public bool Enabled { get; init; }
            public long Created { get; init; }
            public long Updated { get; init; }
            public string RecoveryLevel { get; init; } = "Recoverable+Purgeable";
        }
    }

    public static GetDeletedKeysResponse FromRecords(IEnumerable<DeletedKeyRecord> records, string vaultName)
    {
        var items = records.Select(record =>
        {
            var bundle = record.Bundle!;
            return new DeletedKeyItem
            {
                Kid = bundle.Key?.Kid,
                Attributes = new DeletedKeyItem.DeletedKeyItemAttributes
                {
                    Enabled = bundle.Attributes.Enabled,
                    Created = bundle.Attributes.Created,
                    Updated = bundle.Attributes.Updated
                },
                DeletedDate = record.DeletedDate,
                ScheduledPurgeDate = record.ScheduledPurgeDate,
                RecoveryId = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/deletedkeys/{record.KeyName}"
            };
        }).ToArray();

        return new GetDeletedKeysResponse { Value = items };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
