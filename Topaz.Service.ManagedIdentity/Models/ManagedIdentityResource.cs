using System.Text.Json.Serialization;
using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ManagedIdentity.Models;

public class ManagedIdentityResource
    : ArmResource<ManagedIdentityResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ManagedIdentityResource()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public ManagedIdentityResource(SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        AzureLocation location,
        IDictionary<string, string>? tags,
        ManagedIdentityResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/Microsoft.ManagedIdentity/userAssignedIdentities/{name}";
        Name = name;
        Location = location;
        Tags = tags ?? new Dictionary<string, string>();
        Properties = properties;
    }
    
    public sealed override string Id { get; init; }
    public sealed override string Name { get; init; }
    public override string Type => "Microsoft.ManagedIdentity/userAssignedIdentities";
    public sealed override string Location { get; init; }
    public sealed override IDictionary<string, string> Tags { get; init; }
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public sealed override ManagedIdentityResourceProperties Properties { get; init; }
}