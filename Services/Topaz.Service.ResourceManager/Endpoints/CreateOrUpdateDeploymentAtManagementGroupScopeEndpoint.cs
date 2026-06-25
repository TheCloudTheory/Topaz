using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class CreateOrUpdateDeploymentAtManagementGroupScopeEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator orchestrator) : IEndpointDefinition
{
    private readonly ManagementGroupDeploymentControlPlane _controlPlane =
        new(new ManagementGroupDeploymentResourceProvider(logger), orchestrator,
            new ArmTemplateEngineFacade(logger), logger);

    public string[] Endpoints =>
    [
        "PUT /providers/Microsoft.Management/managementGroups/{groupId}/providers/Microsoft.Resources/deployments/{deploymentName}"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var groupId = path.ExtractValueFromPath(4);
        var deploymentName = path.ExtractValueFromPath(8);

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        content = content.Replace(", template:{", ", \"template\":{");

        logger.LogDebug(nameof(CreateOrUpdateDeploymentAtManagementGroupScopeEndpoint), nameof(GetResponse),
            "Attempting to deserialize into {0}: {1}", nameof(CreateDeploymentRequest), content);

        var request = JsonSerializer.Deserialize<CreateDeploymentRequest>(content, GlobalSettings.JsonOptions);
        if (request?.Properties == null || string.IsNullOrWhiteSpace(request.Properties.Mode))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Location))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _controlPlane.CreateOrUpdate(
            groupId!,
            deploymentName!,
            JsonSerializer.Serialize(request.Properties.Template),
            request.Properties.GetParameterValues(),
            request.Location,
            request.Properties.Mode);

        response.CreateJsonContentResponse(result.Resource!, HttpStatusCode.Created);
    }
}
