using Topaz.ResourceManager;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

internal sealed class VirtualNetworkControlPlane(VirtualNetworkResourceProvider provider, ITopazLogger logger) : IControlPlane
{
    public OperationResult Deploy(GenericResource resource)
    {
        return OperationResult.Success;
    }
}