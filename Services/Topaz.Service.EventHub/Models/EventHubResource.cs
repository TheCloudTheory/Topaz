using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.EventHub.Models;

internal sealed class EventHubResource
    : ArmSubresource<EventHubResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public EventHubResource()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public EventHubResource(SubscriptionIdentifier subscription,
        ResourceGroupIdentifier resourceGroup,
        EventHubNamespaceIdentifier namespaceIdentifier,
        string name,
        EventHubResourceProperties properties)
    {
        Id = $"/subscriptions/{subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.EventHub/namespaces/{namespaceIdentifier}/hubs/{name}";
        Name = name;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.EventHub/namespaces/hubs";
    public override EventHubResourceProperties Properties { get; init; }
    
    public EventHubNamespaceIdentifier GetNamespace()
    {
        return EventHubNamespaceIdentifier.From(Id.Split("/")[8]);
    }
}