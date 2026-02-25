using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Authorization.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Authorization.Endpoints.RoleDefinitions;

public class ListRoleDefinitionsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly AuthorizationControlPlane _controlPlane = AuthorizationControlPlane.New(logger);
    
    public string[] Endpoints => [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions",
        "GET /{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions",
    ];

    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = context.Request.Path.Value.StartsWith("/subscriptions") ? 
            SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2))
            : SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(1));
        
        logger.LogDebug(nameof(ListRoleDefinitionsEndpoint), nameof(GetResponse),
            "Attempting to list role definitions for subscription ID `{1}` and query `{2}`.",
             subscriptionIdentifier, context.Request.QueryString);

        string? roleName = null;
        if (context.Request.QueryString.TryGetValueForKey("$filter", out var filter))
        {
            // A filter is basically an expression looking like this: $filter=roleName eq 'Contributor'
            roleName = ExtractRoleNamerFromFilter(filter);
        }

        var definitions = _controlPlane.ListRoleDefinitionsBySubscription(subscriptionIdentifier, roleName);
        if (definitions.Result != OperationResult.Success || definitions.Resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = new ListSubscriptionRoleDefinitionsResponse
        {
            Value = definitions.Resource.Select(ListSubscriptionRoleDefinitionsResponse.RoleDefinition.From).ToArray()
        };
        
        response.Content = new StringContent(result.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }
    
    private static string? ExtractRoleNamerFromFilter(string? filter)
    {
        if (string.IsNullOrEmpty(filter)) return null;
        var segments = filter.Split(' ');
        
        return segments.Length > 1 ? segments[2].Replace("'", string.Empty) : null;
    }
}