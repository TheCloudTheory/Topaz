using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses.Keys;

public class KeyOperationResponse
{
    [JsonPropertyName("kid")]
    public string? Kid { get; init; }

    [JsonPropertyName("value")]
    public string? Result { get; init; }

    [JsonPropertyName("iv")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Iv { get; init; }

    [JsonPropertyName("tag")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tag { get; init; }

    [JsonPropertyName("aad")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Aad { get; init; }

    public static KeyOperationResponse New(string kid, string result,
        string? iv = null, string? tag = null, string? aad = null)
        => new() { Kid = kid, Result = result, Iv = iv, Tag = tag, Aad = aad };

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
