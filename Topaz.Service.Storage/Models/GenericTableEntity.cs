using System.Text.Json.Serialization;
using Azure;
using Azure.Data.Tables;

namespace Topaz.Service.Storage.Models;

internal sealed class GenericTableEntity : ITableEntity
{
    public string? PartitionKey { get; set; }
    public string? RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }

    [JsonPropertyName("odata.etag")]
    public ETag ETag { get; set; }
}
