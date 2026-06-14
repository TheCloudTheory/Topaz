using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.LoadBalancer;

internal sealed class LoadBalancerResourceProvider(ITopazLogger logger) 
    : ResourceProviderBase<LoadBalancerService>(logger)
{
}
