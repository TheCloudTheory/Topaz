using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Models;

public sealed class SqlDatabaseListResponse
{
    public SqlDatabaseResource[] Value { get; set; } = [];

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
