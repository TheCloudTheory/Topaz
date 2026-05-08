using System.Text.Json.Serialization;

namespace Topaz.Service.KeyVault.Models.Requests.Keys;

public record class GetRandomBytesRequest
{
    [JsonPropertyName("count")]
    public int Count { get; init; }
}
