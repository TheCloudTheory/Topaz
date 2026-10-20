namespace Topaz.Service.Shared;

public sealed class GlobalOptions
{
    public string? CertificateFile { get; init; }
    public string? CertificateKey { get; init; }
    public bool EnableLoggingToFile { get; set; }
    public Guid? DefaultSubscription { get; init; }
    public string? EmulatorIpAddress { get; set; }
    public bool RunWithoutLicense { get; set; }
    public string? LicenseKey { get; set; }
}