using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

internal class GetDeletedSecretResponse
{
    public string? RecoveryId { get; init; }
    public long DeletedDate { get; init; }
    public long ScheduledPurgeDate { get; init; }
    public string? Id { get; init; }
    public string? Value { get; init; }
    public string? ContentType { get; init; }
    public SecretAttributes? Attributes { get; init; }

    public static GetDeletedSecretResponse FromRecord(DeletedSecretRecord record, string vaultName)
    {
        var secret = record.Secret!;
        return new GetDeletedSecretResponse
        {
            Id = secret.Id,
            Value = secret.Value,
            ContentType = secret.ContentType,
            Attributes = secret.Attributes,
            DeletedDate = record.DeletedDate,
            ScheduledPurgeDate = record.ScheduledPurgeDate,
            RecoveryId = $"https://{vaultName}.keyvault.topaz.local.dev/deletedsecrets/{secret.Name}"
        };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
