using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

internal sealed class NetworkInterfaceResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<NetworkInterfaceService>(logger) { }
