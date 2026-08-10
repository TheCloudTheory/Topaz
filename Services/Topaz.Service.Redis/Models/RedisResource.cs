using System.Text.Json.Serialization;
using Azure.ResourceManager.Resources;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

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
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Cache/redis/{name}";
        Name = name;
        Location = location;
        Tags = tags ?? new Dictionary<string, string>();
        Sku = sku;
        Properties = properties;
        
        properties.ConfigureHostname(name);
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.Cache/Redis";
    public override string? Location { get; set; }
    public override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; set; }
    public override string? Kind { get; init; }
    public override RedisResourceProperties Properties { get; init; }

    public static RedisResource From(GenericResourceData data)
    {
        return new RedisResource
        {
            Location = data.Location,
            Tags = data.Tags,
            Sku = new ResourceSku
            {
                Capacity = data.Sku.Capacity,
                Family = data.Sku.Family,
                Name = data.Sku.Name,
                Tier = data.Sku.Tier
            },
            Properties = data.Properties.ToObjectFromJson<RedisResourceProperties>(GlobalSettings.JsonOptions)!
        };
    }
}