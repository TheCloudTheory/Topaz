using Topaz.Service.Entra.Models;
using Topaz.Service.ManagedIdentity.Models.Requests;

namespace Topaz.Service.ManagedIdentity.Models;

public sealed class ManagedIdentityResourceProperties
{
    public string? ClientId { get; set; }
    public string? PrincipalId { get; set; }
    public string? TenantId { get; set; }
    public string? IsolationScope { get; set; }

    internal static ManagedIdentityResourceProperties From(
        CreateUpdateManagedIdentityRequest.ManagedIdentityProperties? properties, ServicePrincipal servicePrincipal)
    {
        return new ManagedIdentityResourceProperties
        {
            ClientId = servicePrincipal.AppId,
            PrincipalId = servicePrincipal.Id,
            TenantId = Guid.NewGuid().ToString(),
            IsolationScope = properties?.IsolationScope
        };
    }
}