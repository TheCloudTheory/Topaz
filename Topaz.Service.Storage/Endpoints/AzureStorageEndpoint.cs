using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Service.Storage.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints;

public class AzureStorageEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly AzureStorageControlPlane _controlPlane = new(new ResourceProvider(logger), logger);

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}",
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}",
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/listKeys"
    ];

    public string[] Permissions => [];

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
                logger.LogDebug(nameof(AzureStorageEndpoint), nameof(GetResponse),
                    "Executing {0}: No account name provided.", nameof(GetResponse));

                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            switch (context.Request.Method)
            {
                case "PUT":
                    HandleCreateOrUpdateStorageAccount(response, subscriptionIdentifier, resourceGroupIdentifier,
                        storageAccountName, context.Request.Body);
                    break;
                case "GET":
                    HandleGetStorageAccount(response, subscriptionIdentifier, resourceGroupIdentifier,
                        storageAccountName);
                    break;
                case "DELETE":
                    HandleDeleteStorageAccount(response, subscriptionIdentifier, resourceGroupIdentifier,
                        storageAccountName);
                    break;
                case "POST":
                    HandleListKeysRequest(response, subscriptionIdentifier, resourceGroupIdentifier,
                        storageAccountName);
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
        }
    }

    private void HandleListKeysRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        var storageAccount = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (storageAccount.result == OperationResult.NotFound || storageAccount.resource == null)
        {
            logger.LogInformation($"Storage account [{storageAccountName}] not found.");
            response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode,
                $"Microsoft.Storage/storageAccounts/{storageAccountName}", resourceGroupIdentifier);

            return;
        }

        var keys = new ListKeysResponse(storageAccount.resource.Keys);

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(keys.ToString());
    }

    private void HandleDeleteStorageAccount(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        var storageAccount = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (storageAccount.result == OperationResult.NotFound || storageAccount.resource == null)
        {
            logger.LogInformation($"Storage account [{storageAccountName}] not found.");

            // That maybe be strange, but according to the documentation, when a user tries to 
            // remove Storage Account which doesn't exist, they should receive HTTP 204 instead
            // of HTTP 404.
            response.StatusCode = HttpStatusCode.NoContent;
            return;
        }

        _controlPlane.Delete(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);

        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleGetStorageAccount(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        var storageAccount = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (storageAccount.result == OperationResult.NotFound || storageAccount.resource == null)
        {
            logger.LogInformation($"Storage account [{storageAccountName}] not found.");
            response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode,
                $"Microsoft.Storage/storageAccounts/{storageAccountName}", resourceGroupIdentifier);

            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(storageAccount.resource.ToString());
    }

    private void HandleCreateOrUpdateStorageAccount(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName, Stream input)
    {
        using var reader = new StreamReader(input);

        var content = reader.ReadToEnd();
        var request =
            JsonSerializer.Deserialize<CreateOrUpdateStorageAccountRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            logger.LogError($"Could not deserialize the request content of {content}.");
            response.StatusCode = HttpStatusCode.InternalServerError;

            return;
        }

        var result = _controlPlane.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
            request);

        response.Content = new StringContent(result.resource.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }
}