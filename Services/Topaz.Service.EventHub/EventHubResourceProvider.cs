using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventHub;

internal sealed class EventHubResourceProvider(ITopazLogger logger) : ResourceProviderBase<EventHubService>(logger)
{
}