using Topaz.ResourceManager;

namespace Topaz.Service.ServiceBus.Models.Requests;

public sealed class CreateOrUpdateServiceBusQueueRequest
{
    public CreateOrUpdateServiceBusQueueRequestProperties? Properties { get; private init; } = new();

    public static CreateOrUpdateServiceBusQueueRequest From(GenericResource resource)
    {
        return new CreateOrUpdateServiceBusQueueRequest
        {
            Properties = resource.Properties as CreateOrUpdateServiceBusQueueRequestProperties
        };
    }

    public static CreateOrUpdateServiceBusQueueRequest From(CreateOrUpdateServiceBusQueueAtomRequest request)
    {
        return new CreateOrUpdateServiceBusQueueRequest
        {
            Properties = request.Content?.Properties
        };
    }
}