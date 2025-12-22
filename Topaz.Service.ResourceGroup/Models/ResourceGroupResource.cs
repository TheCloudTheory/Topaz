using System.Text.Json.Serialization;
using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ResourceGroup.Models;

public sealed class ResourceGroupResource
    : ArmResource<ResourceGroupProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ResourceGroupResource()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }
    
    public ResourceGroupResource(SubscriptionIdentifier subscriptionId,
        string name,
        AzureLocation location,
        ResourceGroupProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{name}";
        Name = name;
        Location = location;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.ResourceGroups/group";
    public override string Location { get; set; }
    public override IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public override ResourceGroupProperties Properties { get; init; }
}