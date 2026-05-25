using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class CancelDeploymentAtTenantScopeEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator orchestrator) : IEndpointDefinition
{
    private readonly TenantDeploymentControlPlane _controlPlane =
        new(new TenantDeploymentResourceProvider(logger), orchestrator, logger);

    public string[] Endpoints =>
    [
        "POST /providers/Microsoft.Resources/deployments/{deploymentName}/cancel"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/cancel/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var deploymentName = path.ExtractValueFromPath(4);

        logger.LogDebug(nameof(CancelDeploymentAtTenantScopeEndpoint), nameof(GetResponse),
            "Cancelling tenant-scope deployment: `{0}`", deploymentName);

        var result = _controlPlane.CancelDeployment(deploymentName!);
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
