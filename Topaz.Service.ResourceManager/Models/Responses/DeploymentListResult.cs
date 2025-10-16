using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models.Responses;

internal sealed record DeploymentListResult(DeploymentResource[] Value)
{
    [UsedImplicitly] public static string? NextLink => "";
    [UsedImplicitly] public DeploymentResource[] Value { get; set; } = Value;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}