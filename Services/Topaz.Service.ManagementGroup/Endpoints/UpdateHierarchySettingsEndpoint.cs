using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ManagementGroup.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ManagementGroup.Endpoints;

internal sealed class UpdateHierarchySettingsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ManagementGroupControlPlane _controlPlane = ManagementGroupControlPlane.New(logger);

    public string[] Endpoints =>
        ["PATCH /providers/Microsoft.Management/managementGroups/{groupId}/settings/default"];

    public string[] Permissions => ["Microsoft.Management/managementGroups/settings/write"];

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
            ? new UpdateHierarchySettingsRequest()
            : JsonSerializer.Deserialize<UpdateHierarchySettingsRequest>(content, GlobalSettings.JsonOptions)
              ?? new UpdateHierarchySettingsRequest();

        var operation = _controlPlane.UpdateHierarchySettings(groupId, request);
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
