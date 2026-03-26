using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus;

internal sealed class ServiceBusResourceProvider(ITopazLogger logger) : ResourceProviderBase<ServiceBusService>(logger) 
{
}