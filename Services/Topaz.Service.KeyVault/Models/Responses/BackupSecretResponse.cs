using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

public class BackupSecretResponse
{
    public string? Value { get; init; }

    public static BackupSecretResponse New(string encodedValue) => new() { Value = encodedValue };

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
