using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ManagedIdentity.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ManagedIdentity;

public sealed class ManagedIdentityEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ManagedIdentityControlPlane _controlPlane = new(
        new ManagedIdentityResourceProvider(logger),
        ResourceGroupControlPlane.New(logger),
        SubscriptionControlPlane.New(logger),
        logger
    );

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{resourceName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{resourceName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities",
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.ManagedIdentity/userAssignedIdentities",
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{resourceName}",
        "PATCH /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{resourceName}"
    ];

    public (int Port, Protocol Protocol) PortAndProtocol => (GlobalSettings.DefaultResourceManagerPort, Protocol.Https);

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers,
        QueryString query,
        GlobalOptions options)
    {
        logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");

        var response = new HttpResponseMessage();

        try
        {
            ResourceGroupIdentifier? resourceGroupIdentifier = null;
            var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
            var resourceGroupName = path.ExtractValueFromPath(4);
            if (!string.IsNullOrEmpty(resourceGroupName))
            {
                resourceGroupIdentifier = ResourceGroupIdentifier.From(resourceGroupName);
            }

            var managedIdentityName = path.ExtractValueFromPath(8);

            switch (method)
            {
                case "PUT":
                    HandleCreateUpdateManagedIdentityRequest(response, subscriptionIdentifier, resourceGroupIdentifier!,
                        managedIdentityName!, input);
                    break;
                default:
                    response.StatusCode = HttpStatusCode.NotFound;
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;

            return response;
        }

        return response;
    }

    private void HandleCreateUpdateManagedIdentityRequest(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string managedIdentityName, Stream input)
    {
        logger.LogDebug($"Executing {nameof(HandleCreateUpdateManagedIdentityRequest)}.");

        using var reader = new StreamReader(input);

        var content = reader.ReadToEnd();
        var request =
            JsonSerializer.Deserialize<CreateUpdateManagedIdentityRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = _controlPlane.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityName,
            request);
        if ((result.Result != OperationResult.Created && result.Result != OperationResult.Updated) ||
            result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }

        response.StatusCode = result.Result == OperationResult.Created ? HttpStatusCode.Created : HttpStatusCode.OK;
        response.Content = new StringContent(result.Resource.ToString());
    }
}