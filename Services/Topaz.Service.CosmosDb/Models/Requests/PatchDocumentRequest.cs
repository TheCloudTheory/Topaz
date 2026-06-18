using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Topaz.Service.CosmosDb.Models.Requests;

/// <summary>
/// Request body for <c>PATCH /dbs/{db}/colls/{coll}/docs/{docId}</c>.
/// Represents the Cosmos DB partial update (patch) operation list.
/// Supported ops: <c>set</c>, <c>add</c>, <c>replace</c>, <c>remove</c>, <c>increment</c>.
/// </summary>
public sealed class PatchDocumentRequest
{
    [JsonPropertyName("operations")]
    public PatchOperation[] Operations { get; init; } = [];
}

public sealed class PatchOperation
{
    [JsonPropertyName("op")]
    public string Op { get; init; } = string.Empty;

    /// <summary>JSON Pointer path (e.g. <c>/field</c> or <c>/nested/field</c>).</summary>
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    /// <summary>New value for <c>set</c>, <c>add</c>, <c>replace</c>, and <c>increment</c> ops.</summary>
    [JsonPropertyName("value")]
    public JsonNode? Value { get; init; }
}
