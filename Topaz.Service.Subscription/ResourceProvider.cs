using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Subscription;

public sealed class ResourceProvider(ITopazLogger logger) : ResourceProviderBase<SubscriptionService>(logger)
{
}
