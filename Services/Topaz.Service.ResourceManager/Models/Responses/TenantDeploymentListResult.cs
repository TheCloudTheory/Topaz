using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Service.ResourceManager.Models;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models.Responses;

internal sealed record TenantDeploymentListResult(TenantDeploymentResource[] Value)
{
    [UsedImplicitly] public static string? NextLink => "";
    [UsedImplicitly] public TenantDeploymentResource[] Value { get; set; } = Value;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
