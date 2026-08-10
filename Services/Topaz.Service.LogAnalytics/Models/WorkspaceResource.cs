using System.Text.Json.Serialization;
using Azure.ResourceManager.Resources;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

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
    public sealed override ResourceSku? Sku { get; set; }
    public sealed override string? Kind { get; init; }
    public sealed override WorkspaceResourceProperties Properties { get; init; }

    public static WorkspaceResource From(GenericResourceData data)
    {
        return new WorkspaceResource
        {
            Location = data.Location,
            Tags = data.Tags,
            Properties = data.Properties.ToObjectFromJson<WorkspaceResourceProperties>(GlobalSettings.JsonOptions)!
        };
    }
}
