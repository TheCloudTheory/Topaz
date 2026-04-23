using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Topaz.Service.Storage.Models;

[UsedImplicitly]
public class QueueProperties
{
    [JsonPropertyName("QueueName")]
    public string? Name { get; init; }

    [JsonPropertyName("CreatedTime")]
    public DateTimeOffset? CreatedTime { get; init; }

    [JsonPropertyName("UpdatedTime")]
    public DateTimeOffset? UpdatedTime { get; init; }

    [JsonPropertyName("ApproximateMessageCount")]
    public int ApproximateMessageCount { get; init; }

    [JsonPropertyName("Metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
}
