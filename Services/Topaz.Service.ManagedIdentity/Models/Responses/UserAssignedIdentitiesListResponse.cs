using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ManagedIdentity.Models.Responses;

public sealed class UserAssignedIdentitiesListResponse
{
    public ManagedIdentityResource[] Value { get; set; } = [];
    public string? NextLink { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}