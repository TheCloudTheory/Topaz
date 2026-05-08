using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses.Certificates;

public class BackupCertificateResponse
{
    public string? Value { get; init; }

    public static BackupCertificateResponse New(string encodedValue) => new() { Value = encodedValue };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
