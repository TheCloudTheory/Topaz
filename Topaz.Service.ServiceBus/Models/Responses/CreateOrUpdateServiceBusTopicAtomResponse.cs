using System.Xml.Serialization;

namespace Topaz.Service.ServiceBus.Models.Responses;

[XmlRoot("entry", Namespace = "http://www.w3.org/2005/Atom", IsNullable = false)]
public class CreateOrUpdateServiceBusTopicAtomResponse
{
    [XmlElement("title")]
    public string? Title { get; init; }
    
    [XmlElement("content")]
    public CreateOrUpdateServiceBusTopicResponseContent? Content { get; init; } = new();

    public class CreateOrUpdateServiceBusTopicResponseContent
    {
        [XmlElement("TopicDescription", Namespace = "http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
        public ServiceBusTopicResourceProperties? Properties { get; init; } = new();
    }

    public static CreateOrUpdateServiceBusTopicAtomResponse From(string topicName, ServiceBusTopicResourceProperties properties)
    {
        return new CreateOrUpdateServiceBusTopicAtomResponse
        {
            Title = topicName,
            Content = new CreateOrUpdateServiceBusTopicResponseContent
            {
                Properties = properties
            }
        };
    }
}