using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ManagedIdentity.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ManagedIdentity.Endpoints.FederatedIdentityCredentials;

internal sealed class CreateOrUpdateFederatedIdentityCredentialEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly FederatedIdentityCredentialControlPlane _controlPlane =
        FederatedIdentityCredentialControlPlane.New(logger);

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{resourceName}/federatedIdentityCredentials/{federatedIdentityCredentialResourceName}"
    ];

    public string[] Permissions => ["Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
        var managedIdentityName = path.ExtractValueFromPath(8)!;
        var ficName = path.ExtractValueFromPath(10)!;

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateOrUpdateFederatedIdentityCredentialRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var operation = _controlPlane.CreateOrUpdate(
            subscriptionIdentifier, resourceGroupIdentifier, managedIdentityName, ficName, request);

        if (operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated)
        {
            response.CreateErrorResponse(operation.Code!, operation.Reason!);
            return;
        }

        var statusCode = operation.Result == OperationResult.Created
            ? HttpStatusCode.Created
            : HttpStatusCode.OK;
        response.CreateJsonContentResponse(operation.Resource!, statusCode);
    }
}
