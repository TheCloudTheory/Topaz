using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

public class DeleteSecretResponse
{
    public string? RecoveryId { get; init; }
    public long DeletedDate { get; init; }
    public long ScheduledPurgeDate { get; init; }
    public string? Id { get; init; }
    public SecretAttributes? Attributes { get; init; }

    public static DeleteSecretResponse New(string id, string vaultName, string secretName, SecretAttributes attributes)
    {
        return new DeleteSecretResponse()
        {
            Id = id,
            Attributes = attributes,
            DeletedDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
            ScheduledPurgeDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
            RecoveryId = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/deletedsecrets/{secretName}"
        };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
