using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

public class BackupKeyResponse
{
    public string? Value { get; init; }

    public static BackupKeyResponse New(string encodedValue) => new() { Value = encodedValue };

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
