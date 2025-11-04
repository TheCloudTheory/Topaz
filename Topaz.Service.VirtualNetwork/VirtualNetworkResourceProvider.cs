using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

internal sealed class VirtualNetworkResourceProvider(ITopazLogger logger) : ResourceProviderBase<VirtualNetworkService>(logger)
{
}