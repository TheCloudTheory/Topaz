using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ManagementGroup.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ManagementGroup.Endpoints;

internal sealed class GetManagementGroupEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ManagementGroupControlPlane _controlPlane = ManagementGroupControlPlane.New(logger);

    public string[] Endpoints => ["GET /providers/Microsoft.Management/managementGroups/{groupId}"];

    public string[] Permissions => ["Microsoft.Management/managementGroups/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var groupId = context.Request.Path.Value.ExtractValueFromPath(4);
        if (string.IsNullOrWhiteSpace(groupId))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }
        
        // The request may contain an optional parameter `$expand`. Per docs,
        // the parameter may contain one of the two values:
        // - `children`
        // - `path`
        // For now, only the former is supported.
        var expandChildren = context.Request.Query.ContainsKey("$expand") &&
                             context.Request.Query["$expand"] == "children";

        var operation = _controlPlane.Get(groupId,  expandChildren);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.CreateJsonContentResponse(
                new { error = new { code = operation.Code, message = operation.Reason } }, HttpStatusCode.NotFound);
            return;
        }

        response.CreateJsonContentResponse(GetManagementGroupResponse.From(operation.Resource));
    }
}
