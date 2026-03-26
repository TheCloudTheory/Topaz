using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.Authorization.Models;

internal sealed class RoleAssignmentResource : ArmResource<RoleAssignmentResourceProperties>
{
    [JsonConstructor]
    #pragma warning disable CS8618
    public RoleAssignmentResource()
    #pragma warning restore CS8618
    {
    }

    public RoleAssignmentResource(SubscriptionIdentifier subscriptionId, string name, RoleAssignmentResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleAssignments/{name}";
        Name = name;
        Properties = properties;
    }

    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.Authorization/roleAssignments";
    public override string? Location { get; set; }
    public override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public override RoleAssignmentResourceProperties Properties { get; init; }
}
