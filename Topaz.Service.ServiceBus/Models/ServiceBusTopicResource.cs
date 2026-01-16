using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.ServiceBus.Models.Responses;
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

    /// <summary>
    /// Returns an XML string representing properties of Service Bus topic.
    /// Used 
    /// </summary>
    public string ToXmlString()
    {
        var serializer = new XmlSerializer(typeof(CreateOrUpdateServiceBusTopicAtomResponse));
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, CreateOrUpdateServiceBusTopicAtomResponse.From(Name, Properties));
        
        return stringWriter.ToString();
    }
}