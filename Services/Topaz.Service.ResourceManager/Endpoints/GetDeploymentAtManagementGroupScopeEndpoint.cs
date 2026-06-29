using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class GetDeploymentAtManagementGroupScopeEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator orchestrator) : IEndpointDefinition
{
    private readonly ManagementGroupDeploymentControlPlane _controlPlane =
        new(new ManagementGroupDeploymentResourceProvider(logger), orchestrator,
            new ArmTemplateEngineFacade(logger), logger);

    public string[] Endpoints =>
    [
        "GET /providers/Microsoft.Management/managementGroups/{groupId}/providers/Microsoft.Resources/deployments/{deploymentName}"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        // index: 0="" 1="providers" 2="Microsoft.Management" 3="managementGroups" 4="{groupId}"
        //        5="providers" 6="Microsoft.Resources" 7="deployments" 8="{deploymentName}"
        var groupId = path.ExtractValueFromPath(4);
        var deploymentName = path.ExtractValueFromPath(8);

        logger.LogDebug(nameof(GetDeploymentAtManagementGroupScopeEndpoint), nameof(GetResponse),
            "Getting management-group-scope deployment: group `{0}`, deployment `{1}`",
            groupId, deploymentName);

        var result = _controlPlane.Get(groupId!, deploymentName!);
        if (result.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new StringContent(result.ToString());
            return;
        }

        response.CreateJsonContentResponse(result.Resource!, HttpStatusCode.OK);
    }
}
