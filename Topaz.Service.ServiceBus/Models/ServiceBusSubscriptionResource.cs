using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.ServiceBus.Models.Responses;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ServiceBus.Models;

internal sealed class ServiceBusSubscriptionResource
    : ArmSubresource<ServiceBusSubscriptionResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ServiceBusSubscriptionResource()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public ServiceBusSubscriptionResource(SubscriptionIdentifier subscription,
        ResourceGroupIdentifier resourceGroup,
        ServiceBusNamespaceIdentifier namespaceIdentifier,
        string name,
        ServiceBusSubscriptionResourceProperties properties)
    {
        Id = $"/subscriptions/{subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.ServiceBus/namespaces/{namespaceIdentifier}/subscriptions/{name}";
        Name = name;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.ServiceBus/namespaces/subscriptions";
    public override ServiceBusSubscriptionResourceProperties Properties { get; init; }

    /// <summary>
    /// Returns an XML string representing properties of Service Bus Subscription.
    /// Used in legacy endpoints based on Atom.
    /// </summary>
    public string ToXmlString()
    {
        var serializer = new XmlSerializer(typeof(CreateOrUpdateServiceBusSubscriptionAtomResponse));
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, CreateOrUpdateServiceBusSubscriptionAtomResponse.From(Name, Properties));
        
        return stringWriter.ToString();
    }
}