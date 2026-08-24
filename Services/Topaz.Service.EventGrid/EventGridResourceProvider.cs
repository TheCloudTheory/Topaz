using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventGrid;

internal sealed class EventGridResourceProvider(ITopazLogger logger) : ResourceProviderBase<EventGridService>(logger);