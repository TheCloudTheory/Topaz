using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

public class GetRandomBytesResponse
{
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    public static GetRandomBytesResponse New(string base64UrlValue) => new() { Value = base64UrlValue };

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
