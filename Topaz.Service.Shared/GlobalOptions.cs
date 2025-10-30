namespace Topaz.Service.Shared;

public sealed class GlobalOptions
{
    public Guid? TenantId { get; init; }
    public string? CertificateFile { get; init; }
    public string? CertificateKey { get; init; }
    public bool SkipRegistrationOfDnsEntries { get; init; }
    public bool EnableLoggingToFile { get; set; }
    public Guid? DefaultSubscription { get; init; }
    public string? EmulatorIpAddress { get; set; }
}