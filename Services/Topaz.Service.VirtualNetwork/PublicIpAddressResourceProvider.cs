using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

internal sealed class PublicIpAddressResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<PublicIpAddressService>(logger) { }
