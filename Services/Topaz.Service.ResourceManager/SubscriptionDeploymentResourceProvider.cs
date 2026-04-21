using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

public sealed class SubscriptionDeploymentResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<SubscriptionDeploymentService>(logger)
{
}
