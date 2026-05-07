using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ManagementGroup.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ManagementGroup.Endpoints;

internal sealed class GetDescendantsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ManagementGroupControlPlane _controlPlane = ManagementGroupControlPlane.New(logger);

    public string[] Endpoints => ["GET /providers/Microsoft.Management/managementGroups/{groupId}/descendants"];

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

        var operation = _controlPlane.GetDescendants(groupId);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode,
                operation.Reason ?? $"Management group '{groupId}' not found.",
                System.Net.HttpStatusCode.NotFound);
            return;
        }

        response.CreateJsonContentResponse(new GetDescendantsResponse(operation.Resource));
    }
}
