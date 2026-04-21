using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ManagementGroup.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ManagementGroup.Endpoints;

internal sealed class UpdateManagementGroupEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ManagementGroupControlPlane _controlPlane = ManagementGroupControlPlane.New(logger);

    public string[] Endpoints => ["PATCH /providers/Microsoft.Management/managementGroups/{groupId}"];

    public string[] Permissions => ["Microsoft.Management/managementGroups/write"];

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

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        var request = string.IsNullOrWhiteSpace(content)
            ? new UpdateManagementGroupRequest()
            : JsonSerializer.Deserialize<UpdateManagementGroupRequest>(content, GlobalSettings.JsonOptions)
              ?? new UpdateManagementGroupRequest();

        var operation = _controlPlane.Update(groupId, request);
        response.StatusCode = operation.Result switch
        {
            OperationResult.Updated => HttpStatusCode.OK,
            OperationResult.NotFound => HttpStatusCode.NotFound,
            OperationResult.Failed => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        if (operation.Resource != null)
        {
            response.CreateJsonContentResponse(operation.Resource);
        }
        else
        {
            response.CreateJsonContentResponse(
                new { error = new { code = operation.Code, message = operation.Reason } });
        }
    }
}
