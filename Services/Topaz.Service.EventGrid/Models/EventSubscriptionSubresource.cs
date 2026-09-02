using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.EventGrid.Models;

internal sealed class EventSubscriptionSubresource : ArmSubresource<EventSubscriptionSubresourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public EventSubscriptionSubresource()
#pragma warning restore CS8618
    {
    }

    public EventSubscriptionSubresource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string topicName,
        string name,
        EventSubscriptionSubresourceProperties properties)
    {
        Id =
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.EventGrid/topics/{topicName}/eventSubscriptions/{name}";
        Name = name;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.EventGrid/topics/eventSubscriptions";
    public override EventSubscriptionSubresourceProperties Properties { get; init; }

    public void UpdateFromRequest(EventSubscriptionSubresourceProperties request)
    {
        Properties.UpdateFromRequest(request);
    }
}