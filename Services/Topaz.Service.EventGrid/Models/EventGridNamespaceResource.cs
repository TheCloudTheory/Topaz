using System.Text.Json.Serialization;
using Azure.ResourceManager.Resources;
using JetBrains.Annotations;
using Topaz.ResourceManager;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.EventGrid.Models;

internal sealed class EventGridNamespaceResource : ArmResource<EventGridNamespaceResourceProperties>, IValidatable
{
    [UsedImplicitly]
    public EventGridNamespaceResource()
    {
    }

    public EventGridNamespaceResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        string location,
        IDictionary<string, string>? tags,
        ResourceSku? sku,
        EventGridNamespaceResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.EventGrid/namespaces/{name}";
        Name = name;
        Location = location;
        Tags = tags ?? new Dictionary<string, string>();
        Sku = sku is not null ? new ResourceSku { Name = sku.Name?.ToString(), Capacity = sku.Capacity } : null;
        Properties = properties;
    }

    public sealed override string Id { get; init; } = null!;
    public sealed override string Name { get; init; } = null!;
    public override string Type { get; init; } = "Microsoft.EventGrid/namespaces";
    public sealed override string? Location { get; set; }
    public sealed override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; set; }
    public override string? Kind { get; init; }
    public sealed override EventGridNamespaceResourceProperties Properties { get; init; } = null!;
    
    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        return string.IsNullOrWhiteSpace(Location) ? (false, "Location is required.") : (true, null);
    }

    public void UpdateFromRequest(EventGridNamespaceResource request)
    {
        Sku = request.Sku ?? Sku;
        
        Properties.UpdateFromRequest(request.Properties);
    }

    public static EventGridNamespaceResource From(GenericResourceData data)
    {
        return new EventGridNamespaceResource
        {
            Name = data.Name,
            Kind = data.Kind,
            Sku = new ResourceSku
            {
                Name = data.Sku.Name,
                Capacity = data.Sku.Capacity
            },
            Tags = data.Tags,
            Properties = data.Properties.ToObjectFromJson<EventGridNamespaceResourceProperties>(GlobalSettings.JsonOptions)!
        };
    }
}
