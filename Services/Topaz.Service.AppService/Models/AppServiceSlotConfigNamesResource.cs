using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.AppService.Models;

internal sealed class AppServiceSlotConfigNamesResource
{
    [JsonConstructor]
    public AppServiceSlotConfigNamesResource() { }

    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = "slotConfigNames";
    public string Type { get; init; } = "Microsoft.Web/sites/config";
    public string? Location { get; init; }
    public SlotConfigNamesProperties Properties { get; init; } = new();

    public static AppServiceSlotConfigNamesResource FromSite(AppServiceSiteResource site) =>
        new()
        {
            Id = $"{site.Id}/config/slotConfigNames",
            Name = "slotConfigNames",
            Type = "Microsoft.Web/sites/config",
            Location = site.Location,
            Properties = new SlotConfigNamesProperties()
        };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    internal sealed class SlotConfigNamesProperties
    {
        public string[] AppSettingNames { get; init; } = [];
        public string[] AzureStorageConfigNames { get; init; } = [];
        public string[] ConnectionStringNames { get; init; } = [];
    }
}
