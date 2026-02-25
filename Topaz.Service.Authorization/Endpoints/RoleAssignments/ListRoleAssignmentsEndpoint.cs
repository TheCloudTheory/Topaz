using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Authorization.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Authorization.Endpoints.RoleAssignments;

public class ListRoleAssignmentsEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly AuthorizationControlPlane _controlPlane = AuthorizationControlPlane.New(eventPipeline, logger);
    
    public string[] Endpoints => [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleAssignments"
    ];

    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        
        logger.LogDebug(nameof(ListRoleAssignmentsEndpoint), nameof(GetResponse),
            "Attempting to list role assignments for subscription ID `{0}`.", subscriptionIdentifier);
        
        string? roleName = null;
        if (context.Request.QueryString.TryGetValueForKey("$filter", out var filter))
        {
            // A filter is basically an expression looking like this: $filter=roleName eq 'Contributor'
            roleName = ExtractRoleNamerFromFilter(filter);
        }
        
        var assignments = _controlPlane.ListRoleAssignmentsBySubscription(subscriptionIdentifier, roleName);
        if (assignments.Result != OperationResult.Success || assignments.Resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = new ListSubscriptionRoleAssignmentsResponse
        {
            Value = assignments.Resource.Select(ListSubscriptionRoleAssignmentsResponse.RoleAssignment.From).ToArray()
        };
        
        response.Content = new StringContent(result.ToString());
        response.StatusCode = HttpStatusCode.OK;
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
    
    private static string? ExtractRoleNamerFromFilter(string? filter)
    {
        if (string.IsNullOrEmpty(filter)) return null;
        var segments = filter.Split(' ');
        
        return segments.Length > 1 ? segments[2].Replace("'", string.Empty) : null;
    }
}