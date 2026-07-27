using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.Redis.Models;

internal sealed class RedisResource : ArmResource<RedisResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public RedisResource()
#pragma warning restore CS8618
    {
    }

    public RedisResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        string location,
        IDictionary<string, string>? tags,
        ResourceSku? sku,
        RedisResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.AppConfiguration/configurationStores/{name}";
        Name = name;
        Location = location;
        Tags = tags ?? new Dictionary<string, string>();
        Sku = sku;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.Cache/Redis";
    public override string? Location { get; set; }
    public override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public override RedisResourceProperties Properties { get; init; }
}