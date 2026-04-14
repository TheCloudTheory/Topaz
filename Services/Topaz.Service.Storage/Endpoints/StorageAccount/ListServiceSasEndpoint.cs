using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.StorageAccount;

internal sealed class ListServiceSasEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly AzureStorageControlPlane _controlPlane = new(new ResourceProvider(logger), logger);

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/ListServiceSas"
    ];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/listServiceSas/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var storageAccountName = context.Request.Path.Value.ExtractValueFromPath(8);

            if (string.IsNullOrWhiteSpace(storageAccountName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            using var reader = new StreamReader(context.Request.Body);
            var content = reader.ReadToEnd();
            var request = JsonSerializer.Deserialize<ListServiceSasRequest>(content, GlobalSettings.JsonOptions);

            if (request == null)
            {
                logger.LogError($"Could not deserialize the ListServiceSas request content: {content}.");
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            if (string.IsNullOrWhiteSpace(request.CanonicalizedResource) ||
                string.IsNullOrWhiteSpace(request.SignedResource) ||
                string.IsNullOrWhiteSpace(request.SignedExpiry))
            {
                logger.LogError("ListServiceSas request is missing required fields (canonicalizedResource, signedResource, signedExpiry).");
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var result = _controlPlane.ListServiceSas(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, request);

            if (result.Result == OperationResult.NotFound)
            {
                response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode,
                    $"Microsoft.Storage/storageAccounts/{storageAccountName}", resourceGroupIdentifier);
                return;
            }

            if (result.Result == OperationResult.Failed || result.Resource == null)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            }

            response.CreateJsonContentResponse(result.Resource);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
