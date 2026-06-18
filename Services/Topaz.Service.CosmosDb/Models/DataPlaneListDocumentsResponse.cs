using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Models;

/// <summary>
/// List-documents response: <c>{"_rid":"...","Documents":[...],"_count":N}</c>
/// </summary>
public sealed class DataPlaneListDocumentsResponse
{
    [JsonPropertyName("_rid")]
    public string Rid { get; init; } = string.Empty;

    [JsonPropertyName("Documents")]
    public JsonObject[] Documents { get; init; } = [];

    [JsonPropertyName("_count")]
    public int Count { get; init; }

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    public static DataPlaneListDocumentsResponse From(JsonObject[] documents, string rid = "") =>
        new() { Rid = rid, Documents = documents, Count = documents.Length };
}
