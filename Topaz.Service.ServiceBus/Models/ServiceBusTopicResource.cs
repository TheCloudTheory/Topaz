using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ServiceBus.Models;

internal sealed class ServiceBusTopicResource
    : ArmSubresource<ServiceBusTopicResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ServiceBusTopicResource()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public ServiceBusTopicResource(SubscriptionIdentifier subscription,
        ResourceGroupIdentifier resourceGroup,
        ServiceBusNamespaceIdentifier namespaceIdentifier,
        string name,
        ServiceBusTopicResourceProperties properties)
    {
        Id = $"/subscriptions/{subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.ServiceBus/namespaces/{namespaceIdentifier}/topics/{name}";
        Name = name;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.ServiceBus/namespaces/topics";
    public override ServiceBusTopicResourceProperties Properties { get; init; }
}