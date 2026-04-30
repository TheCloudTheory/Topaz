using Topaz.Service.ResourceManager.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

internal sealed class TenantDeploymentControlPlane(
    TenantDeploymentResourceProvider provider,
    ITopazLogger logger)
{
    public ControlPlaneOperationResult<TenantDeploymentResource[]> List()
    {
        logger.LogDebug(nameof(TenantDeploymentControlPlane), nameof(List),
            "Listing tenant-scope deployments.");

        var resources = provider.ListDeployments().ToArray();

        return new ControlPlaneOperationResult<TenantDeploymentResource[]>(
            OperationResult.Success, resources, null, null);
    }
}
