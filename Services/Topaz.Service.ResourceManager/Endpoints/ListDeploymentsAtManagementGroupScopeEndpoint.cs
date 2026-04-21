using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class ListDeploymentsAtManagementGroupScopeEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ManagementGroupDeploymentControlPlane _controlPlane =
        new(new ManagementGroupDeploymentResourceProvider(logger), logger);

    public string[] Endpoints =>
    [
        "GET /providers/Microsoft.Management/managementGroups/{groupId}/providers/Microsoft.Resources/deployments",
        "GET /providers/Microsoft.Management/managementGroups/{groupId}/providers/Microsoft.Resources/deployments/"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        // /providers/Microsoft.Management/managementGroups/{groupId}/providers/Microsoft.Resources/deployments
        // index:  0=""  1="providers"  2="Microsoft.Management"  3="managementGroups"  4="{groupId}"
        var groupId = path.ExtractValueFromPath(4);

        if (string.IsNullOrWhiteSpace(groupId))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _controlPlane.List(groupId);

        if (result.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.CreateJsonContentResponse(new ManagementGroupDeploymentListResult(result.Resource!));
    }
}
