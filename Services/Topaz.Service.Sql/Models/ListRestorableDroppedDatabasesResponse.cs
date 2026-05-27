using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Sql.Models;

public sealed class ListRestorableDroppedDatabasesResponse
{
    public object[] Value { get; init; } = [];

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
