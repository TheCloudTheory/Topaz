using Azure.ResourceManager.Resources;
using Topaz.Service.AppService.Models;
using Topaz.Shared;

namespace Topaz.Service.AppService.Models.Requests;

internal sealed record CreateOrUpdateAppServiceSiteRequest
{
    public string? Location { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
    public string? Kind { get; init; }
    public AppServiceSiteProperties? Properties { get; init; }

    internal sealed class AppServiceSiteProperties
    {
        public string? ServerFarmId { get; init; }
        public SiteConfigRequest? SiteConfig { get; init; }
    }

    internal sealed class SiteConfigRequest
    {
        public AppServiceNameValuePair[]? AppSettings { get; init; }
        public AppServiceNameValuePair[]? ConnectionStrings { get; init; }
        public string? LinuxFxVersion { get; init; }
        public string? NetFrameworkVersion { get; init; }
        public bool? AlwaysOn { get; init; }
        public string? FtpsState { get; init; }
        public string? MinTlsVersion { get; init; }
    }

    public static CreateOrUpdateAppServiceSiteRequest From(GenericResourceData data)
    {
        return new CreateOrUpdateAppServiceSiteRequest
        {
            Kind = data.Kind,
            Location = data.Location,
            Tags = data.Tags,
            Properties = data.Properties.ToObjectFromJson<AppServiceSiteProperties>(GlobalSettings.JsonOptions)
        };
    }
}
