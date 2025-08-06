using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Service.Storage.Models.Responses;
using Topaz.Shared;

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
    
    public (int Port, Protocol Protocol) PortAndProtocol => (GlobalSettings.DefaultResourceManagerPort, Protocol.Https);
    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query, GlobalOptions options)
    {
        logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");
        
        var response = new HttpResponseMessage();

        try
        {
            var subscriptionId = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
            var resourceGroupName = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
            var storageAccountName = path.ExtractValueFromPath(8);
            
            if (string.IsNullOrWhiteSpace(storageAccountName))
            {
                logger.LogDebug($"Executing {nameof(GetResponse)}: No account name provided.");
                
                response.StatusCode = HttpStatusCode.BadRequest;
                return response;
            }
            
            switch (method)
            {
                case "PUT":
                    HandleCreateOrUpdateStorageAccount(response, subscriptionId, resourceGroupName, storageAccountName, input);
                    break;
                case "GET":
                    HandleGetStorageAccount(response, resourceGroupName, storageAccountName);
                    break;
                case "DELETE":
                    HandleDeleteStorageAccount(response, storageAccountName);
                    break;
                case "POST":
                    HandleListKeysRequest(response, resourceGroupName, storageAccountName);
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
        
        return response;
    }

    private void HandleListKeysRequest(HttpResponseMessage response, ResourceGroupIdentifier resourceGroup, string storageAccountName)
    {
        var storageAccount = _controlPlane.Get(storageAccountName);
        if (storageAccount.result == OperationResult.NotFound || storageAccount.resource == null)
        {
            logger.LogInformation($"Storage account [{storageAccountName}] not found.");
            response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode, $"Microsoft.Storage/storageAccounts/{storageAccountName}", resourceGroup);
            
            return;
        }
        
        var keys = new ListKeysResponse(storageAccount.resource.Keys);
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(keys.ToString());
    }

    private void HandleDeleteStorageAccount(HttpResponseMessage response, string storageAccountName)
    {
        var storageAccount = _controlPlane.Get(storageAccountName);
        if (storageAccount.result == OperationResult.NotFound || storageAccount.resource == null)
        {
            logger.LogInformation($"Storage account [{storageAccountName}] not found.");
            
            // That maybe be strange, but according to the documentation, when a user tries to 
            // remove Storage Account which doesn't exist, they should receive HTTP 204 instead
            // of HTTP 404.
            response.StatusCode = HttpStatusCode.NoContent;
            return;
        }
        
        _controlPlane.Delete(storageAccountName);
        
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleGetStorageAccount(HttpResponseMessage response, ResourceGroupIdentifier resourceGroup, string storageAccountName)
    {
        var storageAccount = _controlPlane.Get(storageAccountName);
        if (storageAccount.result == OperationResult.NotFound || storageAccount.resource == null)
        {
            logger.LogInformation($"Storage account [{storageAccountName}] not found.");
            response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode, $"Microsoft.Storage/storageAccounts/{storageAccountName}", resourceGroup);
            
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(storageAccount.resource.ToString());
    }

    private void HandleCreateOrUpdateStorageAccount(HttpResponseMessage response, SubscriptionIdentifier subscriptionId, ResourceGroupIdentifier resourceGroup, string storageAccountName, Stream input)
    {
        using var reader = new StreamReader(input);
        
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateOrUpdateStorageAccountRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            logger.LogError($"Could not deserialize the request content of {content}.");
            response.StatusCode = HttpStatusCode.InternalServerError;
            
            return;
        }
        
        var result = _controlPlane.CreateOrUpdate(subscriptionId, resourceGroup, storageAccountName, request);

        response.Content = new StringContent(result.resource.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }
}