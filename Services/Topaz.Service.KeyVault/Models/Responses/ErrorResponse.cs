namespace Topaz.Service.KeyVault.Models.Responses;

public record ErrorResponse
{
    public ErrorData? Error { get; init; }
    
    public record ErrorData
    {
        public string? Code { get; init; }
        public ErrorData? InnerError { get; init; }
        public string? Message { get; init; }
    }
}