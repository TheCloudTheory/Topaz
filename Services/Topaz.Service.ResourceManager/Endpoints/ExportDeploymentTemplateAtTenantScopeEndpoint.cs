using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class ExportDeploymentTemplateAtTenantScopeEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator orchestrator) : IEndpointDefinition
{
    private readonly TenantDeploymentControlPlane _controlPlane =
        new(new TenantDeploymentResourceProvider(logger), orchestrator, logger);

    public string[] Endpoints =>
    [
        "POST /providers/Microsoft.Resources/deployments/{deploymentName}/exportTemplate"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/exportTemplate/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var deploymentName = path.ExtractValueFromPath(4);

        logger.LogDebug(nameof(ExportDeploymentTemplateAtTenantScopeEndpoint), nameof(GetResponse),
            "Exporting template for tenant-scope deployment: `{0}`", deploymentName);

        var result = _controlPlane.ExportDeploymentTemplate(deploymentName!);
        if (result.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new StringContent(result.ToString());
            return;
        }

        if (result.Result == OperationResult.Failed)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            response.Content = new StringContent(result.ToString());
            return;
        }

        response.CreateJsonContentResponse(result.Resource!, HttpStatusCode.OK);
    }
}
