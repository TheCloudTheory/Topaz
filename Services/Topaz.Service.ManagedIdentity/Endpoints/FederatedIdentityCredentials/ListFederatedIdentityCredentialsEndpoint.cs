using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ManagedIdentity.Models;
using Topaz.Service.ManagedIdentity.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ManagedIdentity.Endpoints.FederatedIdentityCredentials;

internal sealed class ListFederatedIdentityCredentialsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly FederatedIdentityCredentialControlPlane _controlPlane =
        FederatedIdentityCredentialControlPlane.New(logger);

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{resourceName}/federatedIdentityCredentials"
    ];

    public string[] Permissions => ["Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
        var managedIdentityName = path.ExtractValueFromPath(8)!;

        var operation = _controlPlane.List(
            subscriptionIdentifier, resourceGroupIdentifier, managedIdentityName);

        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        if (operation.Result != OperationResult.Success || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = new FederatedIdentityCredentialsListResponse { Value = operation.Resource };
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(result.ToString());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
