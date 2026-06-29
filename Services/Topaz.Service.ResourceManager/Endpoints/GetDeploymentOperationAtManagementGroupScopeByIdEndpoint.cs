using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class GetDeploymentOperationAtManagementGroupScopeByIdEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator orchestrator) : IEndpointDefinition
{
    private readonly TemplateDeploymentOrchestrator _orchestrator = orchestrator;
    public string[] Endpoints =>
    [
        "GET /providers/Microsoft.Management/managementGroups/{groupId}/providers/Microsoft.Resources/deployments/{deploymentName}/operations/{operationId}"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        // index: 0="" 1="providers" 2="Microsoft.Management" 3="managementGroups" 4="{groupId}"
        //        5="providers" 6="Microsoft.Resources" 7="deployments" 8="{deploymentName}"
        //        9="operations" 10="{operationId}"
        var groupId = path.ExtractValueFromPath(4);
        var deploymentName = path.ExtractValueFromPath(8);
        var operationId = path.ExtractValueFromPath(10);

        logger.LogDebug(nameof(GetDeploymentOperationAtManagementGroupScopeByIdEndpoint), nameof(GetResponse),
            "Get management-group-scope deployment operation: group `{0}`, deployment `{1}`, operationId `{2}`",
            groupId, deploymentName, operationId);

        var provider = new ManagementGroupDeploymentResourceProvider(logger);
        if (provider.GetDeployment(groupId!, deploymentName!) is null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            return;
        }

        var dir = OperationStore.GetMgScopeDirectory(groupId!, deploymentName!);
        var record = OperationStore.GetAll(dir)
            .FirstOrDefault(r => string.Equals(r.OperationId, operationId, StringComparison.OrdinalIgnoreCase));

        if (record is null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            return;
        }

        response.CreateJsonContentResponse(record);
        response.Content.Headers.ContentType =
            System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
    }
}
