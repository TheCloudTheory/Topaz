namespace Topaz.Service.Shared;

public class GenericErrorResponse(string code, string message)
{
    public ErrorDetail Error { get; init; } = new ErrorDetail(code, message);

    public class ErrorDetail(string code, string message)
    {
        public string Code { get; init; } = code;
        public string Message { get; init; } = message;
    }
}