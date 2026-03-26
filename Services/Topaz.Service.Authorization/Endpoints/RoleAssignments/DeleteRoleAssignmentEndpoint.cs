using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Authorization.Domain;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Authorization.Endpoints.RoleAssignments;

public class DeleteRoleAssignmentEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly AuthorizationControlPlane _controlPlane = AuthorizationControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleAssignments/{roleAssignmentName}"
    ];

    public string[] Permissions => ["Microsoft.Authorization/roleAssignments/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var roleAssignmentName = RoleAssignmentName.From(context.Request.Path.Value.ExtractValueFromPath(6));
        var existingDefinition = _controlPlane.Get(subscriptionIdentifier, roleAssignmentName);

        switch (existingDefinition.Result)
        {
            case OperationResult.NotFound:
                // For some reason, the API for role assignments is supposed to return HTTP 204
                // when a role is already deleted or non-existing
                response.StatusCode = HttpStatusCode.NoContent;
                return;
            case OperationResult.Failed:
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            default:
                var operation = _controlPlane.Delete(subscriptionIdentifier, roleAssignmentName);
                response.StatusCode = HttpStatusCode.OK;
                response.Content = new StringContent(operation.Resource!.ToString());
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                break;
        }
    }
}