using System.Text.Json.Serialization;

namespace Topaz.Service.KeyVault.Models.Requests;

public record class GetRandomBytesRequest
{
    [JsonPropertyName("count")]
    public int Count { get; init; }
}
