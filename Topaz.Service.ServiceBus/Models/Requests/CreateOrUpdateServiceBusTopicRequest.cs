using Topaz.ResourceManager;

namespace Topaz.Service.ServiceBus.Models.Requests;

public sealed class CreateOrUpdateServiceBusTopicRequest
{
    public CreateOrUpdateServiceBusTopicRequestProperties? Properties { get; init; } = new();

    public static CreateOrUpdateServiceBusTopicRequest From(GenericResource resource)
    {
        return new CreateOrUpdateServiceBusTopicRequest
        {
            Properties = resource.Properties as CreateOrUpdateServiceBusTopicRequestProperties
        };
    }
    
    public static CreateOrUpdateServiceBusTopicRequest From(CreateOrUpdateServiceBusTopicAtomRequest request)
    {
        return new CreateOrUpdateServiceBusTopicRequest
        {
            Properties = request.Content?.Properties
        };
    }
}
