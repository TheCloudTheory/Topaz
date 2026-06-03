using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Models;

public sealed class DatabaseAccountKeysResponse
{
    public string? PrimaryMasterKey { get; set; }
    public string? SecondaryMasterKey { get; set; }
    public string? PrimaryReadonlyMasterKey { get; set; }
    public string? SecondaryReadonlyMasterKey { get; set; }

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
