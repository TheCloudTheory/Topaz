using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models;

internal record DeletedKeyRecord
{
    public KeyBundle? Bundle { get; init; }
    public string? KeyName { get; init; }
    public long DeletedDate { get; init; }
    public long ScheduledPurgeDate { get; init; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
