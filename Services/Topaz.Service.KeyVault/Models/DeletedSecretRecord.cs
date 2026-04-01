using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models;

internal record DeletedSecretRecord
{
    public Secret? Secret { get; init; }
    public long DeletedDate { get; init; }
    public long ScheduledPurgeDate { get; init; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
