using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ManagementGroup.Endpoints;

internal sealed class DeleteManagementGroupEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ManagementGroupControlPlane _controlPlane = ManagementGroupControlPlane.New(logger);

    public string[] Endpoints => ["DELETE /providers/Microsoft.Management/managementGroups/{groupId}"];

    public string[] Permissions => ["Microsoft.Management/managementGroups/delete"];

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

        var operation = _controlPlane.Delete(groupId);
        response.StatusCode = operation.Result switch
        {
            OperationResult.Deleted => HttpStatusCode.NoContent,
            OperationResult.NotFound => HttpStatusCode.NotFound,
            _ => HttpStatusCode.BadRequest
        };

        if (operation.Result != OperationResult.Deleted)
        {
            response.CreateJsonContentResponse(
                new { error = new { code = operation.Code, message = operation.Reason } });
        }
        else
        {
            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentType = null;
        }
    }
}
