using Topaz.Service.AppService.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.AppService.Models;

internal sealed class AppServiceSiteResourceProperties
{
    public AppServiceSiteResourceProperties() { }

    private AppServiceSiteResourceProperties(string siteName)
    {
        DefaultHostName = GlobalSettings.GetWebSiteDefaultHostName(siteName);
        HostNames = [DefaultHostName];
        EnabledHostNames = [DefaultHostName];
        HostNameSslStates =
        [
            new HostNameSslState { Name = DefaultHostName, SslState = "Disabled", HostType = "Standard" }
        ];
    }

    public string? DefaultHostName { get; set; }
    public string[]? HostNames { get; set; }
    public string[]? EnabledHostNames { get; set; }
    public HostNameSslState[]? HostNameSslStates { get; set; }
    public string State { get; init; } = "Running";
    public string AvailabilityState { get; init; } = "Normal";
    public string ProvisioningState { get; init; } = "Succeeded";
    public string? ServerFarmId { get; set; }
    public SiteConfigProperties? SiteConfig { get; set; }

    internal sealed class HostNameSslState
    {
        public string? Name { get; set; }
        public string? SslState { get; set; }
        public string? HostType { get; set; }
    }

    public static AppServiceSiteResourceProperties FromRequest(string siteName, CreateOrUpdateAppServiceSiteRequest request)
    {
        var props = request.Properties;
        return new AppServiceSiteResourceProperties(siteName)
        {
            ServerFarmId = props?.ServerFarmId,
            SiteConfig = props?.SiteConfig == null ? null : new SiteConfigProperties
            {
                AppSettings = props.SiteConfig.AppSettings,
                ConnectionStrings = props.SiteConfig.ConnectionStrings,
                LinuxFxVersion = props.SiteConfig.LinuxFxVersion,
                NetFrameworkVersion = props.SiteConfig.NetFrameworkVersion,
                AlwaysOn = props.SiteConfig.AlwaysOn.GetValueOrDefault(false),
                FtpsState = props.SiteConfig.FtpsState,
                MinTlsVersion = props.SiteConfig.MinTlsVersion,
            },
        };
    }

    public static void UpdateFromRequest(AppServiceSiteResource resource, CreateOrUpdateAppServiceSiteRequest request)
    {
        var props = request.Properties;
        if (props == null) return;

        if (props.ServerFarmId != null) resource.Properties.ServerFarmId = props.ServerFarmId;
        if (props.SiteConfig == null) return;

        var siteConfig = resource.Properties.SiteConfig ??= new SiteConfigProperties();
        if (props.SiteConfig.AppSettings != null) siteConfig.AppSettings = props.SiteConfig.AppSettings;
        if (props.SiteConfig.ConnectionStrings != null) siteConfig.ConnectionStrings = props.SiteConfig.ConnectionStrings;
        if (props.SiteConfig.LinuxFxVersion != null) siteConfig.LinuxFxVersion = props.SiteConfig.LinuxFxVersion;
        if (props.SiteConfig.NetFrameworkVersion != null) siteConfig.NetFrameworkVersion = props.SiteConfig.NetFrameworkVersion;
        if (props.SiteConfig.AlwaysOn.HasValue) siteConfig.AlwaysOn = props.SiteConfig.AlwaysOn.Value;
        if (props.SiteConfig.FtpsState != null) siteConfig.FtpsState = props.SiteConfig.FtpsState;
        if (props.SiteConfig.MinTlsVersion != null) siteConfig.MinTlsVersion = props.SiteConfig.MinTlsVersion;
    }
}
