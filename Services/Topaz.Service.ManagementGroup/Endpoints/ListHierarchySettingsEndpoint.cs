using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ManagementGroup.Endpoints;

internal sealed class ListHierarchySettingsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ManagementGroupControlPlane _controlPlane = ManagementGroupControlPlane.New(logger);

    public string[] Endpoints =>
        ["GET /providers/Microsoft.Management/managementGroups/{groupId}/settings"];

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

        var operation = _controlPlane.ListHierarchySettings(groupId);
        if (operation.Result == OperationResult.NotFound)
        {
            response.CreateJsonContentResponse(
                new { error = new { code = operation.Code, message = operation.Reason } },
                HttpStatusCode.NotFound);
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.CreateJsonContentResponse(new ListHierarchySettingsResponse(operation.Resource!));
    }
}

file sealed class ListHierarchySettingsResponse(Models.HierarchySettings[] value)
{
    public Models.HierarchySettings[] Value { get; } = value;

    public override string ToString() => JsonSerializer.Serialize(this, Topaz.Shared.GlobalSettings.JsonOptions);
}
