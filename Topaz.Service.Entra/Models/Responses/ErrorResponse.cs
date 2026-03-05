using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models.Responses;

internal sealed class ErrorResponse
{
    public const string InvalidGrant = "invalid_grant";
    public const string InvalidRequest = "invalid_request";
    public const string InvalidScope = "invalid_scope";
    public const string InvalidClient = "invalid_client";
    
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
    public int[] ErrorCodes { get; set; } = []; 
    public string? Timestamp { get; set; }
    public string? TraceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? ErrorUri { get; set; }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    public static ErrorResponse Create(string error, string description, int[]? errorCodes = null)
    {
        return new ErrorResponse
        {
            Error = error,
            ErrorDescription = description,
            ErrorCodes = errorCodes ?? [],
            Timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss'Z'"),
            TraceId = Guid.NewGuid().ToString(),
            CorrelationId = Guid.NewGuid().ToString(),
            ErrorUri = errorCodes is { Length: > 0 }
                ? $"https://login.microsoftonline.com/error?code={errorCodes[0]}"
                : null
        };
    }
}