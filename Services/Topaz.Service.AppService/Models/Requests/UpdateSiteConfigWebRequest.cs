using Topaz.Service.AppService.Models;

namespace Topaz.Service.AppService.Models.Requests;

internal sealed record UpdateSiteConfigWebRequest
{
    public SiteConfigWebProperties? Properties { get; init; }

    internal sealed class SiteConfigWebProperties
    {
        public AppServiceNameValuePair[]? AppSettings { get; init; }
        public AppServiceNameValuePair[]? ConnectionStrings { get; init; }
        public string? LinuxFxVersion { get; init; }
        public string? NetFrameworkVersion { get; init; }
        public bool? AlwaysOn { get; init; }
        public string? FtpsState { get; init; }
        public string? MinTlsVersion { get; init; }
    }
}
