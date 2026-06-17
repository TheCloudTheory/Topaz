using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Models;

public sealed class DataPlaneListDatabasesResponse
{
    [JsonPropertyName("_rid")]
    public string Rid { get; init; } = string.Empty;

    [JsonPropertyName("Databases")]
    public SqlDatabaseInnerResource[] Databases { get; init; } = [];

    [JsonPropertyName("_count")]
    public int Count { get; init; }

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    public static DataPlaneListDatabasesResponse From(SqlDatabaseInnerResource[] databases) =>
        new() { Databases = databases, Count = databases.Length };
}
