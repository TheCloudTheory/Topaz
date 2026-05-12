using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Authorization.Domain;
using Topaz.Service.Authorization.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Authorization.Endpoints.RoleAssignments;

/// <summary>
/// Handles resource-group-scoped role assignment creation/update.
/// PUT /subscriptions/{subId}/resourceGroups/{rg}/providers/Microsoft.Authorization/roleAssignments/{name}
/// </summary>
internal sealed class CreateUpdateRoleAssignmentAtResourceGroupEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly AuthorizationControlPlane _controlPlane = AuthorizationControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Authorization";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Authorization/roleAssignments/{roleAssignmentName}"
    ];

    public string[] Permissions => ["Microsoft.Authorization/roleAssignments/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        using var reader = new StreamReader(context.Request.Body);

        var path = context.Request.Path.Value!;
        // /subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Authorization/roleAssignments/{2}
        //  [0]=subscriptions [1]=subId [2]=resourceGroups [3]=rg [4]=providers … [7]=assignmentName
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupName = path.ExtractValueFromPath(4);
        var roleAssignmentName = RoleAssignmentName.From(path.ExtractValueFromPath(8));

        var scope = $"/subscriptions/{subscriptionIdentifier.Value}/resourceGroups/{resourceGroupName}";

        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateOrUpdateRoleAssignmentRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var operation = _controlPlane.CreateOrUpdateRoleAssignment(subscriptionIdentifier, roleAssignmentName, request, scope);
        if (operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated ||
            operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                $"Unknown error when performing CreateOrUpdate operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.Created;
        response.Content = new StringContent(operation.Resource.ToString());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
