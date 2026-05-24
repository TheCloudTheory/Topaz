namespace Topaz.Service.AppService.Models;

internal sealed class SiteConfigProperties
{
    public AppServiceNameValuePair[]? AppSettings { get; set; }
    public AppServiceNameValuePair[]? ConnectionStrings { get; set; }
    public string? LinuxFxVersion { get; set; }
    public string? NetFrameworkVersion { get; set; }
    public bool AlwaysOn { get; set; }
    public string? FtpsState { get; set; }
    public string MinTlsVersion { get; set; } = "1.2";
}
