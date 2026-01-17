using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.ServiceBus.Models.Responses;
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
    
    /// <summary>
    /// Returns an XML string representing properties of Service Bus topic.
    /// Used in legacy endpoints based on Atom.
    /// </summary>
    public string ToXmlString()
    {
        var serializer = new XmlSerializer(typeof(CreateOrUpdateServiceBusQueueAtomResponse));
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, CreateOrUpdateServiceBusQueueAtomResponse.From(Name, Properties));
        
        return stringWriter.ToString();
    }
}