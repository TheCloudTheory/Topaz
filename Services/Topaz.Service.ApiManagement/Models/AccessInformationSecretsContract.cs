using Topaz.Service.Shared;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class AccessInformationSecretsContract : TopazApiModel
{
    public bool Enabled { get; set; }
    public string? Id { get; set; }
    public string? PrimaryKey { get; set; }
    public string? SecondaryKey { get; set; }
    public string? PrincipalId { get; set; }

    public static AccessInformationSecretsContract From(TenantAccessResource resource)
    {
        return new AccessInformationSecretsContract
        {
            Enabled = resource.Properties.Enabled,
            Id = resource.Id,
            PrimaryKey = resource.Properties.PrimaryKey,
            SecondaryKey = resource.Properties.SecondaryKey,
            PrincipalId = resource.Properties.PrincipalId,
        };
    }
}