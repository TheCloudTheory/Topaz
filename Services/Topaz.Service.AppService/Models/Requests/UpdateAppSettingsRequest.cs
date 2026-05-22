namespace Topaz.Service.AppService.Models.Requests;

internal sealed record UpdateAppSettingsRequest
{
    public Dictionary<string, string>? Properties { get; init; }
}
