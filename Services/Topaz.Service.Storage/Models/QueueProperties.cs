using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Topaz.Service.Storage.Models;

[UsedImplicitly]
public class QueueProperties
{
    public QueueProperties()
    {
    }

    [JsonPropertyName("QueueName")]
    public string? Name { get; set; }

    [JsonPropertyName("CreatedTime")]
    public DateTimeOffset? CreatedTime { get; set; }

    [JsonPropertyName("UpdatedTime")]
    public DateTimeOffset? UpdatedTime { get; set; }

    [JsonPropertyName("ApproximateMessageCount")]
    public int ApproximateMessageCount { get; set; }

    [JsonPropertyName("Metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}
