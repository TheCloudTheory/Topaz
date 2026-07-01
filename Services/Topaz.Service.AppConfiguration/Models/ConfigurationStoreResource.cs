using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Models;

public sealed class ConfigurationStoreResource : ArmResource<ConfigurationStoreResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public ConfigurationStoreResource()
#pragma warning restore CS8618
    {
    }

    public ConfigurationStoreResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        string location,
        IDictionary<string, string>? tags,
        ResourceSku? sku,
        ConfigurationStoreResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.AppConfiguration/configurationStores/{name}";
        Name = name;
        Location = location;
        Tags = tags ?? new Dictionary<string, string>();
        Sku = sku;
        Properties = properties;
    }

    public sealed override string Id { get; init; }
    public sealed override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.AppConfiguration/configurationStores";
    public sealed override string? Location { get; set; }
    public sealed override IDictionary<string, string>? Tags { get; set; }
    public sealed override ResourceSku? Sku { get; init; }
    public sealed override string? Kind { get; init; }
    public sealed override ConfigurationStoreResourceProperties Properties { get; init; }
}

