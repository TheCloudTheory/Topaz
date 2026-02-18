using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Authorization;

internal sealed class ResourceAuthorizationResourceProvider(ITopazLogger logger) : ResourceProviderBase<ResourceAuthorizationService>(logger);
internal sealed class ResourceGroupAuthorizationResourceProvider(ITopazLogger logger) : ResourceProviderBase<ResourceGroupAuthorizationService>(logger);
internal sealed class SubscriptionAuthorizationResourceProvider(ITopazLogger logger) : ResourceProviderBase<SubscriptionAuthorizationService>(logger);