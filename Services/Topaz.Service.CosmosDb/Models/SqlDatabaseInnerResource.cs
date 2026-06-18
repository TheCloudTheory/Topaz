using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Models;

public sealed class SqlDatabaseInnerResource
{
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("_rid")]
    public string Rid { get; set; } = string.Empty;

    [JsonPropertyName("_self")]
    public string Self { get; set; } = string.Empty;

    [JsonPropertyName("_etag")]
    public string Etag { get; set; } = string.Empty;

    [JsonPropertyName("_colls")]
    public string Colls { get; set; } = "colls/";

    [JsonPropertyName("_users")]
    public string Users { get; set; } = "users/";

    [JsonPropertyName("_ts")]
    public long Timestamp { get; set; }

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    public static SqlDatabaseInnerResource Create(string databaseName)
    {
        var guidBytes = Guid.NewGuid().ToByteArray();
        var rid = Convert.ToBase64String(guidBytes[..4]).Replace('+', '-').Replace('/', '_');
        return new SqlDatabaseInnerResource
        {
            Id = databaseName,
            Rid = rid,
            Self = $"dbs/{rid}/",
            Etag = $"\"{Guid.NewGuid():N}\"",
            Colls = "colls/",
            Users = "users/",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }
}
