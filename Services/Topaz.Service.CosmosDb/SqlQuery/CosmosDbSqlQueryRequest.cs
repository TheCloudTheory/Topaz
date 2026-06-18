using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Topaz.Service.CosmosDb.SqlQuery;

/// <summary>
/// Deserialised body of a Cosmos DB SQL query request
/// (<c>Content-Type: application/query+json</c>).
/// </summary>
internal sealed class CosmosDbSqlQueryRequest
{
    [JsonPropertyName("query")]
    public string Query { get; init; } = string.Empty;

    [JsonPropertyName("parameters")]
    public CosmosDbQueryParameter[]? Parameters { get; init; }
}

internal sealed class CosmosDbQueryParameter
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public JsonNode? Value { get; init; }
}
