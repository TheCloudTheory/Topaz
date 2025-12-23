using System.Text.Json.Serialization;

namespace Topaz.Service.Storage;

internal class TableErrorResponse(string code, string message)
{
    [JsonPropertyName("odata.error")]
    public ErrorDetail Error { get; init; } = new(code, message);

    public class ErrorDetail(string code, string message)
    {
        public string Code { get; init; } = code;
        public ErrorMessage Message { get; init; } = new(message);

        internal class ErrorMessage(string message)
        {
            public string Value { get; init; } = message;
        }
    }
}