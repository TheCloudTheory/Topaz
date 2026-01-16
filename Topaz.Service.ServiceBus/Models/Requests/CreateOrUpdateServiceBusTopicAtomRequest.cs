using System.Xml.Serialization;
using Topaz.ResourceManager;

namespace Topaz.Service.ServiceBus.Models.Requests;

[XmlRoot("entry", Namespace = "http://www.w3.org/2005/Atom", IsNullable = false)]
public sealed class CreateOrUpdateServiceBusTopicAtomRequest
{
    [XmlElement("content")]
    public CreateOrUpdateServiceBusTopicRequestContent? Content { get; init; } = new();

    public class CreateOrUpdateServiceBusTopicRequestContent
    {
        [XmlElement("TopicDescription", Namespace = "http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
        public CreateOrUpdateServiceBusTopicRequestProperties? Properties { get; init; } = new();
    }

    public static CreateOrUpdateServiceBusTopicAtomRequest From(GenericResource resource)
    {
        return new CreateOrUpdateServiceBusTopicAtomRequest
        {
            Content = new CreateOrUpdateServiceBusTopicRequestContent
            {
                Properties = resource.Properties as CreateOrUpdateServiceBusTopicRequestProperties
            }
        };
    }
}
