using System.Xml.Serialization;

namespace Topaz.Service.ServiceBus.Models.Responses;

[XmlRoot("entry", Namespace = "http://www.w3.org/2005/Atom", IsNullable = false)]
public class CreateOrUpdateServiceBusSubscriptionAtomResponse
{
    [XmlElement("title")]
    public string? Title { get; init; }
    
    [XmlElement("content")]
    public CreateOrUpdateServiceBusSubscriptionResponseContent? Content { get; init; } = new();

    public class CreateOrUpdateServiceBusSubscriptionResponseContent
    {
        [XmlElement("SubscriptionDescription", Namespace = "http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
        public ServiceBusSubscriptionResourceProperties? Properties { get; init; } = new();
    }

    public static CreateOrUpdateServiceBusSubscriptionAtomResponse From(string subscriptionName, ServiceBusSubscriptionResourceProperties properties)
    {
        return new CreateOrUpdateServiceBusSubscriptionAtomResponse
        {
            Title = subscriptionName,
            Content = new CreateOrUpdateServiceBusSubscriptionResponseContent
            {
                Properties = properties
            }
        };
    }
}