using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints;

public class AzureStorageEndpoint(ILogger logger) : IEndpointDefinition
{
    private readonly AzureStorageControlPlane _controlPlane = new(new ResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}",
        "GET /subscriptions/{subscriptionId/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}"
    ];
    
    public (int Port, Protocol Protocol) PortAndProtocol => (GlobalSettings.DefaultResourceManagerPort, Protocol.Https);
    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query)
    {
        logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");
        
        var response = new HttpResponseMessage();

        try
        {
            var subscriptionId = path.ExtractValueFromPath(2);
            var resourceGroupName = path.ExtractValueFromPath(4);
            var accountName = path.ExtractValueFromPath(8);
            
            if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(resourceGroupName) ||
                string.IsNullOrWhiteSpace(accountName))
            {
                logger.LogDebug($"Executing {nameof(GetResponse)}: No subscription ID, resource group name or account name provided.");
                
                response.StatusCode = HttpStatusCode.BadRequest;
                return response;
            }
            
            switch (method)
            {
                case "PUT":
                    HandleCreateOrUpdateStorageAccount(response, subscriptionId, resourceGroupName, accountName, input);
                    break;
                case "GET":
                    HandleGetStorageAccount(response, accountName);
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

    private void HandleGetStorageAccount(HttpResponseMessage response, string accountName)
    {
        var storageAccount = _controlPlane.Get(accountName);
        if (storageAccount.result == OperationResult.Failed || storageAccount.resource == null)
        {
            logger.LogError("There was an error getting the Storage Account.");
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(storageAccount.resource.ToString());
    }

    private void HandleCreateOrUpdateStorageAccount(HttpResponseMessage response, string subscriptionId, string resourceGroupName, string accountName, Stream input)
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
        
        var result = _controlPlane.CreateOrUpdate(subscriptionId, resourceGroupName, accountName, request);

        response.Content = new StringContent(result.resource.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }
}