using System.Text.Json.Serialization;
using Topaz.Service.ManagedIdentity.Models.Requests;

namespace Topaz.Service.ManagedIdentity.Models;

public sealed class ManagedIdentityResourceProperties
{
    public string? ClientId { get; set; }

    public string? PrincipalId { get; set; }

    public string? TenantId { get; set; }

    public string? IsolationScope { get; set; }

    public static ManagedIdentityResourceProperties From(CreateUpdateManagedIdentityRequest.ManagedIdentityProperties? properties)
    {
        return new ManagedIdentityResourceProperties
        {
            ClientId = Guid.NewGuid().ToString(),
            PrincipalId = Guid.NewGuid().ToString(),
            TenantId = Guid.NewGuid().ToString(),
            IsolationScope = properties?.IsolationScope
        };
    }
}