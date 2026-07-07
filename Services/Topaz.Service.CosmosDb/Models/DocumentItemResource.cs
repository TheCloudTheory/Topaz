using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Models;

/// <summary>
/// Represents a Cosmos DB document item as stored on disk.
/// The document body is arbitrary user JSON; system fields are injected alongside it.
/// </summary>
[UsedImplicitly]
public sealed class DocumentItemResource
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("_rid")]
    public string Rid { get; set; } = string.Empty;

    [JsonPropertyName("_self")]
    public string Self { get; set; } = string.Empty;

    [JsonPropertyName("_etag")]
    public string Etag { get; set; } = string.Empty;

    [JsonPropertyName("_ts")]
    public long Timestamp { get; set; }
    
    [JsonPropertyName("ttl")]
    public int? Ttl { get; set; }

    /// <summary>
    /// Injects system fields into an arbitrary user-supplied JSON object and returns the merged
    /// <see cref="JsonObject"/>.  The original user body is not mutated.
    /// </summary>
    public static JsonObject Create(JsonObject userBody, string databaseName, string collectionName)
    {
        var guidBytes = Guid.NewGuid().ToByteArray();
        var rid = Convert.ToBase64String(guidBytes[..12]);
        var result = JsonNode.Parse(userBody.ToJsonString())!.AsObject();

        result["_rid"] = rid;
        result["_self"] = $"dbs/{databaseName}/colls/{collectionName}/docs/{rid}/";
        result["_etag"] = $"\"{Guid.NewGuid():N}\"";
        result["_ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return result;
    }

    /// <summary>
    /// Refreshes <c>_etag</c> and <c>_ts</c> in-place on an already-stored document node.
    /// </summary>
    public static void RefreshSystemFields(JsonObject doc)
    {
        doc["_etag"] = $"\"{Guid.NewGuid():N}\"";
        doc["_ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
