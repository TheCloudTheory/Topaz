namespace Topaz.Service.ResourceManager.Models.Requests;

public record ExportTemplateRequest
{
    public string[]? Resources { get; init; }
    public string? Options { get; init; }
}
