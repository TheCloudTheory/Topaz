using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class DeleteDeploymentAtTenantScopeEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator orchestrator) : IEndpointDefinition
{
    private readonly TenantDeploymentControlPlane _controlPlane =
        new(new TenantDeploymentResourceProvider(logger), orchestrator, logger);

    public string[] Endpoints =>
    [
        "DELETE /providers/Microsoft.Resources/deployments/{deploymentName}"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var deploymentName = path.ExtractValueFromPath(4);

        logger.LogDebug(nameof(DeleteDeploymentAtTenantScopeEndpoint), nameof(GetResponse),
            "Deleting tenant-scope deployment: `{0}`", deploymentName);

        var result = _controlPlane.Delete(deploymentName!);
        response.StatusCode = result switch
        {
            OperationResult.NotFound => HttpStatusCode.NotFound,
            _ => HttpStatusCode.NoContent
        };
        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentType = null;
    }
}
