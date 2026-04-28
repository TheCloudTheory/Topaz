using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

internal class DeleteKeyResponse
{
    public string? RecoveryId { get; init; }
    public long DeletedDate { get; init; }
    public long ScheduledPurgeDate { get; init; }
    public JsonWebKey? Key { get; init; }
    public KeyAttributes? Attributes { get; init; }
    public Dictionary<string, string>? Tags { get; init; }

    public static DeleteKeyResponse New(DeletedKeyRecord record, string vaultName, string keyName)
    {
        var bundle = record.Bundle!;
        return new DeleteKeyResponse
        {
            Key = bundle.Key.ToPublicJwk(),
            Attributes = bundle.Attributes,
            Tags = bundle.Tags,
            DeletedDate = record.DeletedDate,
            ScheduledPurgeDate = record.ScheduledPurgeDate,
            RecoveryId = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/deletedkeys/{keyName}"
        };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
