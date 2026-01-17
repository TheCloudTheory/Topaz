using System.Xml.Serialization;

namespace Topaz.Service.ServiceBus.Models.Responses;

[XmlRoot("entry", Namespace = "http://www.w3.org/2005/Atom", IsNullable = false)]
public class CreateOrUpdateServiceBusQueueAtomResponse
{
    [XmlElement("title")]
    public string? Title { get; init; }
    
    [XmlElement("content")]
    public CreateOrUpdateServiceBusQueueResponseContent? Content { get; init; } = new();

    public class CreateOrUpdateServiceBusQueueResponseContent
    {
        [XmlElement("QueueDescription", Namespace = "http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
        public ServiceBusQueueResourceProperties? Properties { get; init; } = new();
    }

    public static CreateOrUpdateServiceBusQueueAtomResponse From(string queueName, ServiceBusQueueResourceProperties properties)
    {
        return new CreateOrUpdateServiceBusQueueAtomResponse
        {
            Title = queueName,
            Content = new CreateOrUpdateServiceBusQueueResponseContent
            {
                Properties = properties
            }
        };
    }
}