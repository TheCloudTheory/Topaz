using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

internal sealed class NetworkSecurityGroupResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<NetworkSecurityGroupService>(logger);
