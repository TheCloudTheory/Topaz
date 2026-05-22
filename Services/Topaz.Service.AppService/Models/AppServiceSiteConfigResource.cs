using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.AppService.Models;

internal sealed class AppServiceSiteConfigResource
{
    [JsonConstructor]
    public AppServiceSiteConfigResource() { }

    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = "web";
    public string Type { get; init; } = "Microsoft.Web/sites/config";
    public string? Location { get; init; }
    public SiteConfigProperties Properties { get; init; } = new();

    public static AppServiceSiteConfigResource FromSite(AppServiceSiteResource site) =>
        new()
        {
            Id = $"{site.Id}/config/web",
            Name = "web",
            Type = "Microsoft.Web/sites/config",
            Location = site.Location,
            Properties = site.Properties.SiteConfig ?? new SiteConfigProperties()
        };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
