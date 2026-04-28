using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

public class KeyOperationResponse
{
    [JsonPropertyName("kid")]
    public string? Kid { get; init; }

    [JsonPropertyName("value")]
    public string? Result { get; init; }

    public static KeyOperationResponse New(string kid, string result) => new() { Kid = kid, Result = result };

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
