using System.Xml.Serialization;
using Topaz.ResourceManager;

namespace Topaz.Service.ServiceBus.Models.Requests;

[XmlRoot("entry", Namespace = "http://www.w3.org/2005/Atom", IsNullable = false)]
public sealed class CreateOrUpdateServiceBusSubscriptionAtomRequest
{
    [XmlElement("content")]
    public CreateOrUpdateServiceBusSubscriptionRequestContent? Content { get; init; } = new();

    public class CreateOrUpdateServiceBusSubscriptionRequestContent
    {
        [XmlElement("SubscriptionDescription", Namespace = "http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
        public CreateOrUpdateServiceBusSubscriptionRequestProperties? Properties { get; init; } = new();
    }

    public static CreateOrUpdateServiceBusSubscriptionAtomRequest From(GenericResource resource)
    {
        return new CreateOrUpdateServiceBusSubscriptionAtomRequest
        {
            Content = new CreateOrUpdateServiceBusSubscriptionRequestContent
            {
                Properties = resource.Properties as CreateOrUpdateServiceBusSubscriptionRequestProperties
            }
        };
    }
}
