using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventGrid;

internal sealed class EventGridNamespaceResourceProvider(ITopazLogger logger) : ResourceProviderBase<EventGridNamespaceService>(logger);