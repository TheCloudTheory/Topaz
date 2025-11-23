using System.Text.Json.Serialization;
using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.VirtualNetwork.Models;

public sealed class VirtualNetworkResource
    : ArmResource<VirtualNetworkResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public VirtualNetworkResource()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public VirtualNetworkResource(SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        AzureLocation location,
        VirtualNetworkResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Network/virtualNetworks/{name}";
        Name = name;
        Location = location;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.Network/virtualNetworks";
    public override string Location { get; init; }
    public override IDictionary<string, string> Tags { get; init; } =  new Dictionary<string, string>();
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public override VirtualNetworkResourceProperties Properties { get; init; }
}