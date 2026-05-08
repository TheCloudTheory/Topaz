using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models;

internal record DeletedCertificateRecord
{
    public CertificateBundle? Bundle { get; init; }
    public long DeletedDate { get; init; }
    public long ScheduledPurgeDate { get; init; }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
