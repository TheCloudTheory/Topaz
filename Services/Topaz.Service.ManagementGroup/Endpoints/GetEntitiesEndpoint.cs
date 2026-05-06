using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ManagementGroup.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup.Endpoints;

internal sealed class GetEntitiesEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ManagementGroupControlPlane _controlPlane = ManagementGroupControlPlane.New(logger);

    public string[] Endpoints => ["GET /providers/Microsoft.Management/getEntities"];

    public string[] Permissions => ["Microsoft.Management/managementGroups/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var operation = _controlPlane.GetEntities();
        if (operation.Result == OperationResult.Failed || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        response.CreateJsonContentResponse(new GetEntitiesResponse(operation.Resource));
    }
}
