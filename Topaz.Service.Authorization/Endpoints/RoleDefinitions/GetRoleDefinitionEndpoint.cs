using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Authorization.Domain;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Authorization.Endpoints.RoleDefinitions;

internal sealed class GetRoleDefinitionEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly AuthorizationControlPlane _controlPlane = AuthorizationControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{roleDefinitionId}"
    ];

    public string[] Permissions => ["Microsoft.Authorization/roleDefinitions/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var roleDefinitionIdentifier =
            RoleDefinitionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(6));

        logger.LogDebug(nameof(GetRoleDefinitionEndpoint), nameof(GetResponse),
            "Looking for role {0} in subscription {1}.", roleDefinitionIdentifier, subscriptionIdentifier);

        var operation = _controlPlane.Get(subscriptionIdentifier, roleDefinitionIdentifier);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource.ToString());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}