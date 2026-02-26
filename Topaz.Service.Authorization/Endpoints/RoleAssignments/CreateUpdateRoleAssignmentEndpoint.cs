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

internal sealed class CreateUpdateRoleAssignmentEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly AuthorizationControlPlane _controlPlane = AuthorizationControlPlane.New(eventPipeline, logger);
    
    public string[] Endpoints => [
        "PUT /subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleAssignments/{roleAssignmentName}",
        "PUT /{subscriptionId}/providers/Microsoft.Authorization/roleAssignments/{roleAssignmentName}"
    ];

    public string[] Permissions => ["Microsoft.Authorization/roleAssignments/write"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        using var reader = new StreamReader(context.Request.Body);

        var path = context.Request.Path.Value;
        var subscriptionIdentifier = path.StartsWith("/subscriptions") ?
            SubscriptionIdentifier.From(path.ExtractValueFromPath(2))
            : SubscriptionIdentifier.From(path.ExtractValueFromPath(1));
        
        var roleAssignmentName = path.StartsWith("/subscriptions") ? 
            RoleAssignmentName.From(path.ExtractValueFromPath(6))
            : RoleAssignmentName.From(path.ExtractValueFromPath(5));
        
        var content = reader.ReadToEnd();
        var request =
            JsonSerializer.Deserialize<CreateOrUpdateRoleAssignmentRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var operation = _controlPlane.CreateOrUpdateRoleAssignment(subscriptionIdentifier, roleAssignmentName, request);
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