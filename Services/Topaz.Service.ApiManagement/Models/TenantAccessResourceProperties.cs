namespace Topaz.Service.ApiManagement.Models;

internal sealed class TenantAccessResourceProperties
{
    public bool Enabled { get; set; }
    public string? Id { get; set; }
    public string? PrincipalId { get; set; }
    public string? PrimaryKey { get; set; }
    public string? SecondaryKey { get; set; }
}