using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class GetDeploymentOperationAtTenantScopeByIdEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator orchestrator) : IEndpointDefinition
{
    private readonly TemplateDeploymentOrchestrator _orchestrator = orchestrator;

    public string[] Endpoints =>
    [
        "GET /providers/Microsoft.Resources/deployments/{deploymentName}/operations/{operationId}"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        // index: 0="" 1="providers" 2="Microsoft.Resources" 3="deployments" 4="{deploymentName}"
        //        5="operations" 6="{operationId}"
        var deploymentName = path.ExtractValueFromPath(4);
        var operationId = path.ExtractValueFromPath(6);

        logger.LogDebug(nameof(GetDeploymentOperationAtTenantScopeByIdEndpoint), nameof(GetResponse),
            "Get tenant-scope deployment operation: deployment `{0}`, operationId `{1}`",
            deploymentName, operationId);

        var provider = new TenantDeploymentResourceProvider(logger);
        if (provider.GetDeployment(deploymentName!) is null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            return;
        }

        var dir = OperationStore.GetTenantScopeDirectory(deploymentName!);
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
