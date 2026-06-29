using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class GetDeploymentOperationsAtManagementGroupScopeEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator orchestrator) : IEndpointDefinition
{
    private readonly TemplateDeploymentOrchestrator _orchestrator = orchestrator;

    public string[] Endpoints =>
    [
        "GET /providers/Microsoft.Management/managementGroups/{groupId}/providers/Microsoft.Resources/deployments/{deploymentName}/operations"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        // index: 0="" 1="providers" 2="Microsoft.Management" 3="managementGroups" 4="{groupId}"
        //        5="providers" 6="Microsoft.Resources" 7="deployments" 8="{deploymentName}" 9="operations"
        var groupId = path.ExtractValueFromPath(4);
        var deploymentName = path.ExtractValueFromPath(8);

        logger.LogDebug(nameof(GetDeploymentOperationsAtManagementGroupScopeEndpoint), nameof(GetResponse),
            "Get management-group-scope deployment operations: group `{0}`, deployment `{1}`",
            groupId, deploymentName);

        var provider = new ManagementGroupDeploymentResourceProvider(logger);
        if (provider.GetDeployment(groupId!, deploymentName!) is null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            return;
        }

        var dir = OperationStore.GetMgScopeDirectory(groupId!, deploymentName!);
        var operations = OperationStore.GetAll(dir);
        response.CreateJsonContentResponse(new DeploymentOperationsListResult(operations));
    }

    private sealed class DeploymentOperationsListResult(IReadOnlyList<Models.OperationRecord> value)
    {
        public IReadOnlyList<Models.OperationRecord> Value { get; } = value;

        public override string ToString() =>
            System.Text.Json.JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
