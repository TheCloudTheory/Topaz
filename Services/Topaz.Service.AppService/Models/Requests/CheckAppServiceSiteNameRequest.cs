namespace Topaz.Service.AppService.Models.Requests;

internal sealed record CheckAppServiceSiteNameRequest
{
    public required string Name { get; init; }
    public string? Type { get; init; }
}
