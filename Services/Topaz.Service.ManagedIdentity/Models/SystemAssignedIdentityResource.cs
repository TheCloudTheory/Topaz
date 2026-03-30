using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.ManagedIdentity.Models;

public sealed class SystemAssignedIdentityResource
{
    [JsonConstructor]
    public SystemAssignedIdentityResource()
    {
    }

    public SystemAssignedIdentityResource(string parentResourceId, string principalId, string tenantId)
    {
        Id = $"{parentResourceId}/providers/Microsoft.ManagedIdentity/Identities/default";
        Properties = new SystemAssignedIdentityProperties
        {
            PrincipalId = principalId,
            TenantId = tenantId,
            ClientSecretUrl = null
        };
    }

    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = "default";
    public string Type { get; init; } = "Microsoft.ManagedIdentity/Identities";
    public IDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();

    public SystemAssignedIdentityProperties Properties { get; init; } = new();

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

public sealed class SystemAssignedIdentityProperties
{
    public string? PrincipalId { get; init; }
    public string? TenantId { get; init; }
    public string? ClientSecretUrl { get; init; }
}
