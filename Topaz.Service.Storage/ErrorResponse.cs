using System.Text.Json.Serialization;

namespace Topaz.Service.Storage;

internal class ErrorResponse(string code, string message)
{
    [JsonPropertyName("odata.error")]
    public ErrorDetail Error { get; init; } = new ErrorDetail(code, message);

    public class ErrorDetail(string code, string message)
    {
        public string Code { get; init; } = code;
        public ErrorMessage Message { get; init; } = new ErrorMessage(message);

        internal class ErrorMessage(string message)
        {
            public string Value { get; init; } = message;
        }
    }
}