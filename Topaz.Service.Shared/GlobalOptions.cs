namespace Topaz.Service.Shared;

public sealed class GlobalOptions
{
    public Guid? TenantId { get; init; }
    public string? CertificateFile { get; set; }
    public string? CertificateKey { get; set; }
}