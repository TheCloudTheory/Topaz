using System.Xml.Serialization;
using Topaz.ResourceManager;

namespace Topaz.Service.ServiceBus.Models.Requests;

[XmlRoot("entry", Namespace = "http://www.w3.org/2005/Atom", IsNullable = false)]
public sealed class CreateOrUpdateServiceBusQueueAtomRequest
{
    [XmlElement("content")]
    public CreateOrUpdateServiceBusQueueRequestContent? Content { get; init; } = new();

    public class CreateOrUpdateServiceBusQueueRequestContent
    {
        [XmlElement("QueueDescription", Namespace = "http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
        public CreateOrUpdateServiceBusQueueRequestProperties? Properties { get; init; } = new();
    }

    public static CreateOrUpdateServiceBusQueueAtomRequest From(GenericResource resource)
    {
        return new CreateOrUpdateServiceBusQueueAtomRequest
        {
            Content = new CreateOrUpdateServiceBusQueueRequestContent
            {
                Properties = resource.Properties as CreateOrUpdateServiceBusQueueRequestProperties
            }
        };
    }
}
