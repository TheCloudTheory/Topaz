using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.LoadBalancer.Models;

public sealed class LoadBalancerResource
    : ArmResource<LoadBalancerResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public LoadBalancerResource()
#pragma warning restore CS8618
    {
    }

    public LoadBalancerResource(SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        AzureLocation location,
        LoadBalancerResourceProperties properties,
        ResourceSku? sku = null,
        IDictionary<string, string>? tags = null)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Network/loadBalancers/{name}";
        Name = name;
        Location = location.ToString();
        Tags = tags ?? new Dictionary<string, string>();
        Properties = properties;
        Sku = sku;
    }

    public sealed override string Id { get; init; }
    public sealed override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.Network/loadBalancers";
    public sealed override string? Location { get; set; }
    public sealed override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public sealed override LoadBalancerResourceProperties Properties { get; init; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
