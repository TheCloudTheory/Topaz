using Azure.Local.Service.Shared;
using Azure.Local.Shared;

namespace Azure.Local.Service.Subscription;

public sealed class ResourceProvider(ILogger logger) : ResourceProviderBase<SubscriptionService>(logger)
{
}
