using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ServiceBus.Models;

internal sealed class ServiceBusNamespaceResource
    : ArmResource<ServiceBusNamespaceResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ServiceBusNamespaceResource()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public ServiceBusNamespaceResource(SubscriptionIdentifier subscription,
        ResourceGroupIdentifier resourceGroup,
        string location,
        string name,
        ServiceBusNamespaceResourceProperties properties)
    {
        Id = $"/subscriptions/{subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.ServiceBus/namespaces/{name}";
        Name = name;
        Location = location;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.ServiceBus/namespaces";
    public override string Location { get; init; }
    public override IDictionary<string, string> Tags { get; } = new Dictionary<string, string>();
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public override ServiceBusNamespaceResourceProperties Properties { get; init; }
}