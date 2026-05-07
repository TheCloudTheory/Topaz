using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ManagementGroup.Endpoints;

internal sealed class GetHierarchySettingsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ManagementGroupControlPlane _controlPlane = ManagementGroupControlPlane.New(logger);

    public string[] Endpoints =>
        ["GET /providers/Microsoft.Management/managementGroups/{groupId}/settings/default"];

    public string[] Permissions => ["Microsoft.Management/managementGroups/settings/read"];

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

        var operation = _controlPlane.GetHierarchySettings(groupId);
        if (operation.Resource != null)
        {
            response.CreateJsonContentResponse(operation.Resource);
        }
        else
        {
            var status = operation.Result == OperationResult.NotFound
                ? HttpStatusCode.NotFound
                : HttpStatusCode.InternalServerError;
            response.CreateJsonContentResponse(
                new { error = new { code = operation.Code, message = operation.Reason } }, status);
        }
    }
}
