using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Authorization.Domain;
using Topaz.Service.Authorization.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Authorization.Endpoints.RoleAssignments;

/// <summary>
/// Handles management-group-scoped role assignment creation/update.
/// PUT /providers/Microsoft.Management/managementGroups/{mgId}/providers/Microsoft.Authorization/roleAssignments/{name}
/// </summary>
internal sealed class CreateUpdateRoleAssignmentAtManagementGroupEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly AuthorizationControlPlane _controlPlane = AuthorizationControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Authorization";

    public string[] Endpoints =>
    [
        "PUT /providers/Microsoft.Management/managementGroups/{managementGroupId}/providers/Microsoft.Authorization/roleAssignments/{roleAssignmentName}"
    ];

    public string[] Permissions => ["Microsoft.Authorization/roleAssignments/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        using var reader = new StreamReader(context.Request.Body);

        var path = context.Request.Path.Value!;
        // /providers/Microsoft.Management/managementGroups/{mgId}/providers/Microsoft.Authorization/roleAssignments/{name}
        // [0]=""  [1]="providers"  [2]="Microsoft.Management"  [3]="managementGroups"  [4]=mgId
        // [5]="providers"  [6]="Microsoft.Authorization"  [7]="roleAssignments"  [8]=name
        var managementGroupId = path.ExtractValueFromPath(4) ?? string.Empty;
        var roleAssignmentName = RoleAssignmentName.From(path.ExtractValueFromPath(8));

        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateOrUpdateRoleAssignmentRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var operation = _controlPlane.CreateManagementGroupRoleAssignment(managementGroupId, roleAssignmentName, request);
        if (operation.Result != OperationResult.Created || operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                "Unknown error when performing management group role assignment operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.Created;
        response.Content = new StringContent(operation.Resource.ToString());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
