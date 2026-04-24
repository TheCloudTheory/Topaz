using System.Text;

namespace Topaz.Service.Storage.Utils;

internal static class QueueMessageValidator
{
    // Azure Queue Storage hard limit: 64 KB
    private const int MaxMessageSizeBytes = 65536;

    // Visibility timeout: 0-7 days (in seconds)
    private const int MinVisibilityTimeout = 0;
    private const int MaxVisibilityTimeout = 604800; // 7 days

    /// <summary>
    /// Validate message content size. Azure Queue Storage has a 64 KB limit for messages.
    /// Messages are base64-encoded, which adds ~33% overhead.
    /// </summary>
    /// <param name="content">Base64-encoded message content</param>
    /// <param name="errorMessage">Error description if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateMessageSize(string? content, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrEmpty(content))
        {
            return true; // Empty messages are allowed
        }

        var contentBytes = Encoding.UTF8.GetByteCount(content);

        // Check encoded size against 64 KB limit
        // Encoding validation is deferred to the endpoint
        if (contentBytes <= MaxMessageSizeBytes) return true;
        errorMessage = $"Message size exceeds maximum allowed size of {MaxMessageSizeBytes} bytes. " +
                       $"Current size: {contentBytes} bytes.";
        return false;
    }

    /// <summary>
    /// Validate visibility timeout value.
    /// </summary>
    /// <param name="visibilityTimeout">Timeout in seconds</param>
    /// <param name="errorMessage">Error description if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateVisibilityTimeout(int visibilityTimeout, out string? errorMessage)
    {
        errorMessage = null;

        if (visibilityTimeout >= MinVisibilityTimeout && visibilityTimeout <= MaxVisibilityTimeout) return true;
        errorMessage = $"Visibility timeout must be between {MinVisibilityTimeout} and {MaxVisibilityTimeout} seconds. " +
                       $"Current value: {visibilityTimeout} seconds.";
        return false;
    }

    /// <summary>
    /// Get the HTTP status code for message size violation.
    /// </summary>
    public static System.Net.HttpStatusCode GetPayloadTooLargeStatusCode() => (System.Net.HttpStatusCode)413;

    /// <summary>
    /// Get the error code name for message size violation.
    /// </summary>
    public static string GetPayloadTooLargeErrorCode() => "RequestBodyTooLarge";
}
