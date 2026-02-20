using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.CloudEnvironment.Models.Responses;

internal sealed class TokenResponse
{
    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; } = 3600;

    [JsonPropertyName("ext_expires_in")]
    public int ExtExpiresIn { get; init; } = 0;

    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    [JsonPropertyName("id_token")]
    public string? IdToken { get; init; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
