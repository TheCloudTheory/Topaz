using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ManagedIdentity.Endpoints.FederatedIdentityCredentials;

internal sealed class DeleteFederatedIdentityCredentialEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly FederatedIdentityCredentialControlPlane _controlPlane =
        FederatedIdentityCredentialControlPlane.New(logger);

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{resourceName}/federatedIdentityCredentials/{federatedIdentityCredentialResourceName}"
    ];

    public string[] Permissions => ["Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
        var managedIdentityName = path.ExtractValueFromPath(8)!;
        var ficName = path.ExtractValueFromPath(10)!;

        var operation = _controlPlane.Delete(
            subscriptionIdentifier, resourceGroupIdentifier, managedIdentityName, ficName);

        response.StatusCode = operation.Result switch
        {
            OperationResult.Deleted => HttpStatusCode.OK,
            OperationResult.NotFound => HttpStatusCode.NotFound,
            _ => HttpStatusCode.InternalServerError
        };

        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
