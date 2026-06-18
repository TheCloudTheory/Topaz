using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Models;

public sealed class DataPlaneListCollectionsResponse
{
    [JsonPropertyName("_rid")]
    public string Rid { get; init; } = string.Empty;

    [JsonPropertyName("DocumentCollections")]
    public SqlContainerInnerResource[] DocumentCollections { get; init; } = [];

    [JsonPropertyName("_count")]
    public int Count { get; init; }

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    public static DataPlaneListCollectionsResponse From(SqlContainerInnerResource[] collections) =>
        new() { DocumentCollections = collections, Count = collections.Length };
}
