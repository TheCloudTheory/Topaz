using System.Text.Json.Serialization;

namespace Topaz.Portal.Models.Auth;

public sealed class AuthErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    // Some providers use camelCase (your sample)
    [JsonPropertyName("errorDescription")]
    public string? ErrorDescription { get; init; }

    // Some use snake_case (common OAuth2)
    [JsonPropertyName("error_description")]
    public string? ErrorDescriptionSnakeCase { get; init; }

    [JsonPropertyName("errorCodes")]
    public int[] ErrorCodes { get; init; } = [];

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; init; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }

    [JsonPropertyName("errorUri")]
    public string? ErrorUri { get; init; }

    public string? GetBestDescription()
        => !string.IsNullOrWhiteSpace(ErrorDescription) ? ErrorDescription
            : !string.IsNullOrWhiteSpace(ErrorDescriptionSnakeCase) ? ErrorDescriptionSnakeCase
            : null;
}