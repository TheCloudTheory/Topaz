using System.Text.Json.Serialization;

namespace Topaz.Service.KeyVault.Models.Requests;

public record class KeyOperationRequest
{
    [JsonPropertyName("alg")]
    public string? Algorithm { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }

    /// <summary>Initialization vector (AES-CBC only). Base64url-encoded.</summary>
    [JsonPropertyName("iv")]
    public string? Iv { get; init; }

    /// <summary>Additional authenticated data (AES-GCM only). Base64url-encoded.</summary>
    [JsonPropertyName("aad")]
    public string? Aad { get; init; }
}
