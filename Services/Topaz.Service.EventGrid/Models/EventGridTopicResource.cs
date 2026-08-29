using JetBrains.Annotations;
using Topaz.ResourceManager;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.EventGrid.Models;

internal sealed class EventGridTopicResource : ArmResource<EventGridTopicResourceProperties>, IValidatable
{
    [UsedImplicitly]
    public EventGridTopicResource()
    {
    }

    public EventGridTopicResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        string location,
        IDictionary<string, string>? tags,
        EventGridTopicResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.EventGrid/topics/{name}";
        Name = name;
        Location = location;
        Tags = tags ?? new Dictionary<string, string>();
        Properties = properties;
    }

    public override string Id { get; init; } = null!;
    public override string Name { get; init; } = null!;
    public override string Type { get; init; } = "Microsoft.EventGrid/namespaces";
    public override string? Location { get; set; }
    public override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; set; }
    public override string? Kind { get; init; }
    public override EventGridTopicResourceProperties Properties { get; init; } = new();
    
    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        return string.IsNullOrWhiteSpace(Location) ? (false, "Location is required.") : new ValueTuple<bool, string?>(true, null);
    }

    public void UpdateFromRequest(EventGridTopicResource request)
    {
        Tags = request.Tags ?? Tags;
        
        Properties.UpdateFromRequest(request.Properties);
    }
}