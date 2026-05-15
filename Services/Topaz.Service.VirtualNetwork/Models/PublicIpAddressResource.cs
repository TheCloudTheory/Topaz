using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.VirtualNetwork.Models;

public sealed class PublicIpAddressResource : ArmResource<PublicIpAddressResourceProperties>
{
    [JsonConstructor]
    public PublicIpAddressResource() { }

    public PublicIpAddressResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        string location,
        IDictionary<string, string>? tags,
        PublicIpAddressResourceProperties properties,
        ResourceSku? sku = null)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Network/publicIPAddresses/{name}";
        Name = name;
        Location = location;
        Tags = tags ?? new Dictionary<string, string>();
        Properties = properties;
        Sku = sku;
    }

    public sealed override string Id { get; init; } = string.Empty;
    public sealed override string Name { get; init; } = string.Empty;
    public override string Type { get; init; } = "Microsoft.Network/publicIPAddresses";
    public sealed override string? Location { get; set; }
    public sealed override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public sealed override PublicIpAddressResourceProperties Properties { get; init; } = new();
}
