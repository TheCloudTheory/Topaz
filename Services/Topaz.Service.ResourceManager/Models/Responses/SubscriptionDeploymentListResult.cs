using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models.Responses;

internal sealed record SubscriptionDeploymentListResult(SubscriptionDeploymentResource[] Value)
{
    [UsedImplicitly] public static string? NextLink => "";
    [UsedImplicitly] public SubscriptionDeploymentResource[] Value { get; set; } = Value;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
