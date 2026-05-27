using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models.Responses;

internal sealed class DeviceCodeResponse
{
    [JsonPropertyName("device_code")]
    public string DeviceCode { get; init; } = string.Empty;

    [JsonPropertyName("user_code")]
    public string UserCode { get; init; } = string.Empty;

    [JsonPropertyName("verification_uri")]
    public string VerificationUri { get; init; } = "https://topaz.local.dev:8899/devicelogin";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; } = 1800;

    [JsonPropertyName("interval")]
    public int Interval { get; init; } = 5;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    public static DeviceCodeResponse Create(string deviceCode, string userCode)
    {
        return new DeviceCodeResponse
        {
            DeviceCode = deviceCode,
            UserCode = userCode,
            Message = $"To sign in, use a web browser to open the page https://topaz.local.dev:8899/devicelogin and enter the code {userCode} to authenticate."
        };
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
