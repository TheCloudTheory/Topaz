namespace Topaz.Service.AppService.Models;

internal sealed class SiteConfigProperties
{
    public Dictionary<string, string>? AppSettings { get; set; }
    public Dictionary<string, string>? ConnectionStrings { get; set; }
    public string? LinuxFxVersion { get; set; }
    public string? NetFrameworkVersion { get; set; }
    public bool AlwaysOn { get; set; }
    public string? FtpsState { get; set; }
    public string? MinTlsVersion { get; set; }
}
