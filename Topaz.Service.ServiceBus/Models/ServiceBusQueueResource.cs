using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ServiceBus.Models;

internal sealed class ServiceBusQueueResource
    : ArmSubresource<ServiceBusQueueResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ServiceBusQueueResource()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public ServiceBusQueueResource(SubscriptionIdentifier subscription,
        ResourceGroupIdentifier resourceGroup,
        ServiceBusNamespaceIdentifier namespaceIdentifier,
        string name,
        ServiceBusQueueResourceProperties properties)
    {
        Id = $"/subscriptions/{subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.ServiceBus/namespaces/{namespaceIdentifier}/queues/{name}";
        Name = name;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.ServiceBus/namespaces/queues";
    public override ServiceBusQueueResourceProperties Properties { get; init; }

    public ServiceBusNamespaceIdentifier GetNamespace()
    {
        return ServiceBusNamespaceIdentifier.From(Id.Split("/")[8]);
    }
}