using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

internal class GetDeletedKeyResponse
{
    public string? RecoveryId { get; init; }
    public long DeletedDate { get; init; }
    public long ScheduledPurgeDate { get; init; }
    public string? Id { get; init; }
    public JsonWebKey? Key { get; init; }
    public KeyAttributes? Attributes { get; init; }

    public static GetDeletedKeyResponse FromRecord(DeletedKeyRecord record, string vaultName)
    {
        var bundle = record.Bundle!;
        return new GetDeletedKeyResponse
        {
            Id = bundle.Key?.Kid,
            Key = bundle.Key.ToPublicJwk(),
            Attributes = bundle.Attributes,
            DeletedDate = record.DeletedDate,
            ScheduledPurgeDate = record.ScheduledPurgeDate,
            RecoveryId = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/deletedkeys/{record.KeyName}"
        };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
