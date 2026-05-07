using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ManagementGroup.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ManagementGroup.Endpoints;

internal sealed class CreateOrUpdateHierarchySettingsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ManagementGroupControlPlane _controlPlane = ManagementGroupControlPlane.New(logger);

    public string[] Endpoints =>
        ["PUT /providers/Microsoft.Management/managementGroups/{groupId}/settings/default"];

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
            ? new CreateOrUpdateHierarchySettingsRequest()
            : JsonSerializer.Deserialize<CreateOrUpdateHierarchySettingsRequest>(content, GlobalSettings.JsonOptions)
              ?? new CreateOrUpdateHierarchySettingsRequest();

        var operation = _controlPlane.CreateOrUpdateHierarchySettings(groupId, request);
        if (operation.Result == OperationResult.NotFound)
        {
            response.CreateJsonContentResponse(
                new { error = new { code = operation.Code, message = operation.Reason } },
                HttpStatusCode.NotFound);
            return;
        }

        response.CreateJsonContentResponse(operation.Resource!);
    }
}
