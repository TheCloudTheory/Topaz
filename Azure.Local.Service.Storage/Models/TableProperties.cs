using System.Text.Json.Serialization;

namespace Azure.Local.Service.Storage.Models;

internal class TableProperties(string tableName)
{
    [JsonPropertyName("TableName")]
    public string TableName { get; set; } = tableName;
}
