using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class ValidateDeploymentAtManagementGroupScopeEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator orchestrator) : IEndpointDefinition
{
    private readonly ManagementGroupDeploymentControlPlane _controlPlane =
        new(new ManagementGroupDeploymentResourceProvider(logger), orchestrator,
            new ArmTemplateEngineFacade(logger), logger);

    public string[] Endpoints =>
    [
        "POST /providers/Microsoft.Management/managementGroups/{groupId}/providers/Microsoft.Resources/deployments/{deploymentName}/validate"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/validate/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        // index: 0="" 1="providers" 2="Microsoft.Management" 3="managementGroups" 4="{groupId}"
        //        5="providers" 6="Microsoft.Resources" 7="deployments" 8="{deploymentName}" 9="validate"
        var groupId = path.ExtractValueFromPath(4);
        var deploymentName = path.ExtractValueFromPath(8);

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        content = content.Replace(", template:{", ", \"template\":{");

        var request = JsonSerializer.Deserialize<CreateDeploymentRequest>(content, GlobalSettings.JsonOptions);
        if (request == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _controlPlane.ValidateDeployment(groupId!, deploymentName!, request);
        if (result.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            return;
        }

        if (result.Result == OperationResult.Failed)
        {
            response.CreateErrorResponse(result.Code ?? "InvalidTemplate",
                result.Reason ?? "Validation failed.", HttpStatusCode.BadRequest);
            return;
        }

        response.CreateJsonContentResponse(result.Resource!);
    }
}
