using Topaz.Service.ResourceManager.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

internal sealed class ManagementGroupDeploymentControlPlane(
    ManagementGroupDeploymentResourceProvider provider,
    ITopazLogger logger)
{
    private const string NotFoundCode = "ManagementGroupNotFound";
    private const string NotFoundMessageTemplate = "Management group '{0}' was not found.";

    public ControlPlaneOperationResult<ManagementGroupDeploymentResource[]> List(string groupId)
    {
        logger.LogDebug(nameof(ManagementGroupDeploymentControlPlane), nameof(List),
            "Listing management-group-scope deployments for management group '{0}'.", groupId);

        if (!provider.ManagementGroupExists(groupId))
        {
            return new ControlPlaneOperationResult<ManagementGroupDeploymentResource[]>(
                OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, groupId),
                NotFoundCode);
        }

        var resources = provider.ListDeployments(groupId)
            .Where(d => d.IsInManagementGroup(groupId))
            .ToArray();

        return new ControlPlaneOperationResult<ManagementGroupDeploymentResource[]>(
            OperationResult.Success, resources, null, null);
    }
}
