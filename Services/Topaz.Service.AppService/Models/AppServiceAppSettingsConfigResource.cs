using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.AppService.Models;

internal sealed class AppServiceAppSettingsConfigResource
{
    [JsonConstructor]
    public AppServiceAppSettingsConfigResource() { }

    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = "appsettings";
    public string Type { get; init; } = "Microsoft.Web/sites/config";
    public string? Location { get; init; }
    public Dictionary<string, string> Properties { get; init; } = new();

    public static AppServiceAppSettingsConfigResource FromSite(AppServiceSiteResource site) =>
        new()
        {
            Id = $"{site.Id}/config/appsettings",
            Name = "appsettings",
            Type = "Microsoft.Web/sites/config",
            Location = site.Location,
            Properties = (site.Properties.SiteConfig?.AppSettings ?? [])
                .Where(pair => pair.Name != null)
                .ToDictionary(pair => pair.Name!, pair => pair.Value ?? string.Empty)
        };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
