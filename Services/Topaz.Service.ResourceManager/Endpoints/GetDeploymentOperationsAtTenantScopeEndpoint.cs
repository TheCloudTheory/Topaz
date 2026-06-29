using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class GetDeploymentOperationsAtTenantScopeEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator orchestrator) : IEndpointDefinition
{
    private readonly TemplateDeploymentOrchestrator _orchestrator = orchestrator;

    public string[] Endpoints =>
    [
        "GET /providers/Microsoft.Resources/deployments/{deploymentName}/operations"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        // index: 0="" 1="providers" 2="Microsoft.Resources" 3="deployments" 4="{deploymentName}" 5="operations"
        var deploymentName = path.ExtractValueFromPath(4);

        logger.LogDebug(nameof(GetDeploymentOperationsAtTenantScopeEndpoint), nameof(GetResponse),
            "Get tenant-scope deployment operations: deployment `{0}`", deploymentName);

        var provider = new TenantDeploymentResourceProvider(logger);
        if (provider.GetDeployment(deploymentName!) is null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            return;
        }

        var dir = OperationStore.GetTenantScopeDirectory(deploymentName!);
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
