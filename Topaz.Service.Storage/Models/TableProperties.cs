using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Topaz.Service.Storage.Models;

[UsedImplicitly]
internal class TableProperties
{
    [JsonPropertyName("TableName")]
    public string? Name { get; init; }
}
