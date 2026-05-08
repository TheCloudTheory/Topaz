using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models;

internal record DeletedCertificateRecord
{
    public string CertName { get; init; } = string.Empty;
    public CertificateBundle[] Bundles { get; init; } = [];
    public long DeletedDate { get; init; }
    public long ScheduledPurgeDate { get; init; }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
