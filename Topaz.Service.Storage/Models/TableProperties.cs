using System.Text.Json.Serialization;

namespace Topaz.Service.Storage.Models;

internal class TableProperties(string tableName)
{
    [JsonPropertyName("TableName")]
    public string TableName { get; set; } = tableName;
}
