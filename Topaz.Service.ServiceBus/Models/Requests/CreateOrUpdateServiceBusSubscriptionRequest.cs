using Topaz.ResourceManager;

namespace Topaz.Service.ServiceBus.Models.Requests;

public sealed class CreateOrUpdateServiceBusSubscriptionRequest
{
    public CreateOrUpdateServiceBusSubscriptionRequestProperties? Properties { get; private init; } = new();

    public static CreateOrUpdateServiceBusSubscriptionRequest From(GenericResource resource)
    {
        return new CreateOrUpdateServiceBusSubscriptionRequest
        {
            Properties = resource.Properties as CreateOrUpdateServiceBusSubscriptionRequestProperties
        };
    }

    public static CreateOrUpdateServiceBusSubscriptionRequest From(CreateOrUpdateServiceBusSubscriptionAtomRequest request)
    {
        return new CreateOrUpdateServiceBusSubscriptionRequest
        {
            Properties = request.Content?.Properties
        };
    }
}