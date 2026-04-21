using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ManagementGroup.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ManagementGroup.Endpoints;

internal sealed class CreateOrUpdateManagementGroupEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ManagementGroupControlPlane _controlPlane = ManagementGroupControlPlane.New(logger);

    public string[] Endpoints => ["PUT /providers/Microsoft.Management/managementGroups/{groupId}"];

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
            ? new CreateOrUpdateManagementGroupRequest()
            : JsonSerializer.Deserialize<CreateOrUpdateManagementGroupRequest>(content, GlobalSettings.JsonOptions)
              ?? new CreateOrUpdateManagementGroupRequest();

        var operation = _controlPlane.CreateOrUpdate(groupId, request);
        if (operation.Result == OperationResult.Failed)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            response.CreateJsonContentResponse(new { error = new { code = operation.Code, message = operation.Reason } });
            return;
        }

        response.StatusCode = operation.Result == OperationResult.Created
            ? HttpStatusCode.Created
            : HttpStatusCode.OK;

        response.CreateJsonContentResponse(operation.Resource!);
    }
}
