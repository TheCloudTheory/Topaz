using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.VirtualNetwork.Models;

public sealed class NetworkInterfaceResource : ArmResource<NetworkInterfaceResourceProperties>
{
    [JsonConstructor]
    public NetworkInterfaceResource() { }

    public NetworkInterfaceResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        string location,
        IDictionary<string, string>? tags,
        NetworkInterfaceResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Network/networkInterfaces/{name}";
        Name = name;
        Location = location;
        Tags = tags ?? new Dictionary<string, string>();
        Properties = properties;
    }

    public sealed override string Id { get; init; } = string.Empty;
    public sealed override string Name { get; init; } = string.Empty;
    public override string Type { get; init; } = "Microsoft.Network/networkInterfaces";
    public sealed override string? Location { get; set; }
    public sealed override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public sealed override NetworkInterfaceResourceProperties Properties { get; init; } = new();
}
