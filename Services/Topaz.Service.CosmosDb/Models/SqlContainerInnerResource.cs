using System.Text.Json.Serialization;

namespace Topaz.Service.CosmosDb.Models;

public sealed class SqlContainerInnerResource
{
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("_rid")]
    public string Rid { get; set; } = string.Empty;

    [JsonPropertyName("_self")]
    public string Self { get; set; } = string.Empty;

    [JsonPropertyName("_etag")]
    public string Etag { get; set; } = string.Empty;

    [JsonPropertyName("_ts")]
    public long Timestamp { get; set; }

    public ContainerPartitionKey? PartitionKey { get; set; }
    public object? IndexingPolicy { get; set; }
    public object? UniqueKeyPolicy { get; set; }
    public int? DefaultTtl { get; set; }

    public static SqlContainerInnerResource Create(string containerName, ContainerPartitionKey? partitionKey, object? indexingPolicy, object? uniqueKeyPolicy, int? defaultTtl)
    {
        var rid = Convert.ToBase64String(Guid.NewGuid().ToByteArray())[..8];
        return new SqlContainerInnerResource
        {
            Id = containerName,
            Rid = rid,
            Self = $"colls/{rid}/",
            Etag = $"\"{Guid.NewGuid():N}\"",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            PartitionKey = partitionKey,
            IndexingPolicy = indexingPolicy,
            UniqueKeyPolicy = uniqueKeyPolicy,
            DefaultTtl = defaultTtl
        };
    }
}
