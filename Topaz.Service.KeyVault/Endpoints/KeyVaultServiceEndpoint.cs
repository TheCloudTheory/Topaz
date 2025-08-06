using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Endpoints;

public class KeyVaultServiceEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultControlPlane _controlPlane = new(new ResourceProvider(logger));
    public string[] Endpoints => [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}"
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
            var keyVaultName = path.ExtractValueFromPath(8);
            
            switch (method)
            {
                case "PUT":
                    if (string.IsNullOrWhiteSpace(keyVaultName))
                    {
                        logger.LogDebug($"Executing {nameof(GetResponse)}: Can't process request if Key Vault name is empty.");
                        response.StatusCode = HttpStatusCode.BadRequest;
                        break;
                    }
                    
                    HandleCreateUpdateKeyVaultRequest(response, subscriptionId, resourceGroupName, keyVaultName, input);
                    break;
                case "GET":
                    if (string.IsNullOrWhiteSpace(keyVaultName))
                    {
                        logger.LogDebug($"Executing {nameof(GetResponse)}: Can't process request if Key Vault name is empty.");
                        response.StatusCode = HttpStatusCode.BadRequest;
                        break;
                    }
                    
                    HandleGetKeyVaultRequest(response, keyVaultName);
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

    private void HandleGetKeyVaultRequest(HttpResponseMessage response, string keyVaultName)
    {
        var vault = _controlPlane.Get(keyVaultName);
        if (vault.result == OperationResult.Failed || vault.resource == null)
        {
            logger.LogError("There was an error getting the Key Vault.");
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(vault.resource.ToString());
    }

    private void HandleCreateUpdateKeyVaultRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup, string keyVaultName, Stream input)
    {
        using var reader = new StreamReader(input);

        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateOrUpdateKeyVaultRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = _controlPlane.CreateOrUpdate(subscriptionId, resourceGroup, keyVaultName, request);

        response.StatusCode = result.result == OperationResult.Created ? HttpStatusCode.Created : HttpStatusCode.OK;

        // TODO: Once Key Vault is created, response should include full ARM response
        response.Content = new StringContent(result.resource.ToString());
    }
}