using JetBrains.Annotations;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.EventGrid.Models;

internal sealed class EventGridTopicResource : ArmResource<EventGridTopicResourceProperties>
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

    public override required string Id { get; init; }
    public override required string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.EventGrid/namespaces";
    public override string? Location { get; set; }
    public override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; set; }
    public override string? Kind { get; init; }
    public override required EventGridTopicResourceProperties Properties { get; init; }
}