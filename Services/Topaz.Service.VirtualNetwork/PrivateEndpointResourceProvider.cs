using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

internal sealed class PrivateEndpointResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<PrivateEndpointService>(logger);