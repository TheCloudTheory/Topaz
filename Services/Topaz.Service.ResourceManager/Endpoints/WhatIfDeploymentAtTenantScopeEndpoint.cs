using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class WhatIfDeploymentAtTenantScopeEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator deploymentOrchestrator) : IEndpointDefinition
{
    private readonly ResourceManagerControlPlane _controlPlane =
        new(new ResourceManagerResourceProvider(logger), deploymentOrchestrator, logger);

    public string[] Endpoints =>
    [
        "POST /providers/Microsoft.Resources/deployments/{deploymentName}/whatif"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/whatIf/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var deploymentName = path.ExtractValueFromPath(4);

        logger.LogDebug(nameof(WhatIfDeploymentAtTenantScopeEndpoint), nameof(GetResponse),
            "Tenant-scope what-if for deployment: `{0}`", deploymentName);

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        content = content.Replace(", template:{", ", \"template\":{");

        var request = JsonSerializer.Deserialize<CreateDeploymentRequest>(content, GlobalSettings.JsonOptions);
        if (request == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _controlPlane.WhatIfDeploymentAtTenantScope(deploymentName!, request);
        if (result.Result == OperationResult.Failed)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            response.Content = new StringContent(result.ToString());
            return;
        }

        response.CreateJsonContentResponse(result.Resource!, HttpStatusCode.OK);
    }
}
