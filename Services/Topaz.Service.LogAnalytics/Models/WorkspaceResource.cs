using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.LogAnalytics.Models;

public sealed class WorkspaceResource : ArmResource<WorkspaceResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public WorkspaceResource()
#pragma warning restore CS8618
    {
    }

    public WorkspaceResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        string location,
        IDictionary<string, string>? tags,
        WorkspaceResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.OperationalInsights/workspaces/{name}";
        Name = name;
        Location = location;
        Tags = tags ?? new Dictionary<string, string>();
        Properties = properties;
    }

    public sealed override string Id { get; init; }
    public sealed override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.OperationalInsights/workspaces";
    public sealed override string? Location { get; set; }
    public sealed override IDictionary<string, string>? Tags { get; set; }
    public sealed override ResourceSku? Sku { get; init; }
    public sealed override string? Kind { get; init; }
    public sealed override WorkspaceResourceProperties Properties { get; init; }
}
