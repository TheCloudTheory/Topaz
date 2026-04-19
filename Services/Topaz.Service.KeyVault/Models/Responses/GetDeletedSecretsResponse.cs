using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

internal sealed class GetDeletedSecretsResponse
{
    public DeletedSecretItem[]? Value { get; init; }
    public string NextLink { get; init; } = string.Empty;

    public class DeletedSecretItem
    {
        public string? RecoveryId { get; init; }
        public long DeletedDate { get; init; }
        public long ScheduledPurgeDate { get; init; }
        public string? Id { get; init; }
        public string? ContentType { get; init; }
        public DeletedSecretAttributes? Attributes { get; init; }

        public class DeletedSecretAttributes
        {
            public bool Enabled { get; init; }
            public long Created { get; init; }
            public long Updated { get; init; }
            public string RecoveryLevel { get; init; } = "Recoverable+Purgeable";
        }
    }

    public static GetDeletedSecretsResponse FromRecords(IEnumerable<DeletedSecretRecord> records, string vaultName)
    {
        var items = records.Select(record =>
        {
            var secret = record.Secret!;
            return new DeletedSecretItem
            {
                Id = secret.Id,
                ContentType = secret.ContentType,
                Attributes = new DeletedSecretItem.DeletedSecretAttributes
                {
                    Enabled = secret.Attributes.Enabled,
                    Created = secret.Attributes.Created,
                    Updated = secret.Attributes.Updated
                },
                DeletedDate = record.DeletedDate,
                ScheduledPurgeDate = record.ScheduledPurgeDate,
                RecoveryId = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/deletedsecrets/{secret.Name}"
            };
        }).ToArray();

        return new GetDeletedSecretsResponse { Value = items };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
