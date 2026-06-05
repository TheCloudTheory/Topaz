using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class CancelDeploymentAtManagementGroupScopeEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator orchestrator) : IEndpointDefinition
{
    private readonly ManagementGroupDeploymentControlPlane _controlPlane =
        new(new ManagementGroupDeploymentResourceProvider(logger), orchestrator, logger);

    public string[] Endpoints =>
    [
        "POST /providers/Microsoft.Management/managementGroups/{groupId}/providers/Microsoft.Resources/deployments/{deploymentName}/cancel"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/cancel/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        // /providers/Microsoft.Management/managementGroups/{groupId}/providers/Microsoft.Resources/deployments/{deploymentName}/cancel
        // index: 0="" 1="providers" 2="Microsoft.Management" 3="managementGroups" 4="{groupId}"
        //        5="providers" 6="Microsoft.Resources" 7="deployments" 8="{deploymentName}" 9="cancel"
        var groupId = path.ExtractValueFromPath(4);
        var deploymentName = path.ExtractValueFromPath(8);

        if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(deploymentName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            response.Content = new ByteArrayContent([]);
            return;
        }

        logger.LogDebug(nameof(CancelDeploymentAtManagementGroupScopeEndpoint), nameof(GetResponse),
            "Cancelling management-group-scope deployment '{0}' in management group '{1}'.",
            deploymentName, groupId);

        var result = _controlPlane.CancelDeployment(groupId, deploymentName);

        response.StatusCode = result switch
        {
            OperationResult.NotFound => HttpStatusCode.NotFound,
            OperationResult.Conflict => HttpStatusCode.Conflict,
            _ => HttpStatusCode.NoContent
        };
        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentType = null;
    }
}
