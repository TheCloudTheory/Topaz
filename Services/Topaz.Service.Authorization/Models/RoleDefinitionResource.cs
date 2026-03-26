using System.Text.Json.Serialization;
using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.Authorization.Models;

internal sealed class RoleDefinitionResource : ArmResource<RoleDefinitionResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public RoleDefinitionResource()
#pragma warning RoleDefinitionResource CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public RoleDefinitionResource(SubscriptionIdentifier subscriptionId,
        string name,
        RoleDefinitionResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{name}";
        Name = name;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.Authorization/roleDefinitions";
    public override string? Location { get; set; }
    public override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public override RoleDefinitionResourceProperties Properties { get; init; }
}