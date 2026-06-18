using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Models;

/// <summary>
/// SQL query response: <c>{"_rid":"...","Documents":[...],"_count":N}</c>.
/// Uses <see cref="JsonNode"/> for <c>Documents</c> items so that projections
/// (<c>SELECT VALUE</c>) and aggregate scalars can be represented alongside
/// full document objects.
/// </summary>
internal sealed class QueryDocumentsResponse
{
    [JsonPropertyName("_rid")]
    public string Rid { get; init; } = string.Empty;

    [JsonPropertyName("Documents")]
    public JsonNode[] Documents { get; init; } = [];

    [JsonPropertyName("_count")]
    public int Count { get; init; }

    /// <summary>
    /// Not serialised. Carries the skip offset for the next page to the endpoint
    /// so it can write the <c>x-ms-continuation</c> response header.
    /// </summary>
    [JsonIgnore]
    public int? NextSkip { get; init; }

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
