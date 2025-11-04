using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Endpoints;

internal sealed class KeyVaultServiceEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultControlPlane _controlPlane = new(new KeyVaultResourceProvider(logger),
        new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger),
            new SubscriptionControlPlane(new SubscriptionResourceProvider(logger)), logger), logger);
    
    public string[] Endpoints => [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}",
        "GET /subscriptions/{subscriptionId}/resources",
        "POST /subscriptions/{subscriptionId}/providers/Microsoft.KeyVault/checkNameAvailability",
        "POST /subscriptions/{subscriptionId}/providers/Microsoft.KeyVault/checkMhsmNameAvailability",
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}"
    ];
    public (int Port, Protocol Protocol) PortAndProtocol => (GlobalSettings.DefaultResourceManagerPort, Protocol.Https);
    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query, GlobalOptions options)
    {
        logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");
        
        var response = new HttpResponseMessage();

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
            var resourceGroupSegment = path.ExtractValueFromPath(4);
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
                    
                    HandleCreateUpdateKeyVaultRequest(response, subscriptionIdentifier, ResourceGroupIdentifier.From(resourceGroupSegment), keyVaultName, input);
                    break;
                case "GET":
                   
                    if (query.TryGetValueForKey("$filter", out var filter))
                    {
                        HandleListSubscriptionResourcesRequest(subscriptionIdentifier, filter, response);
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(keyVaultName))
                        {
                            logger.LogDebug($"Executing {nameof(GetResponse)}: Can't process request if Key Vault name is empty.");
                            response.StatusCode = HttpStatusCode.BadRequest;
                            break;
                        }
                        
                        HandleGetKeyVaultRequest(response, subscriptionIdentifier, ResourceGroupIdentifier.From(resourceGroupSegment), keyVaultName);
                    }
                    
                    break;
                case "POST":
                    HandleCheckNameRequest(response, subscriptionIdentifier, input);
                    break;
                case "DELETE":
                    if (string.IsNullOrWhiteSpace(keyVaultName))
                    {
                        logger.LogDebug($"Executing {nameof(GetResponse)}: Can't process request if Key Vault name is empty.");
                        response.StatusCode = HttpStatusCode.BadRequest;
                        break;
                    }
                    
                    HandleDeleteKeyVaultRequest(response, subscriptionIdentifier, ResourceGroupIdentifier.From(resourceGroupSegment), keyVaultName);
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

    private void HandleDeleteKeyVaultRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,  string keyVaultName)
    {
        var existingKeyVault = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName);
        switch (existingKeyVault.Result)
        {
            case OperationResult.NotFound:
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            case OperationResult.Failed:
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            default:
                _controlPlane.Delete(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName);
                response.StatusCode = HttpStatusCode.OK;
                break;
        }
    }

    private void HandleCheckNameRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, Stream input)
    {
        using var reader = new StreamReader(input);

        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CheckNameKeyVaultRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = _controlPlane.CheckName(subscriptionIdentifier, request.Name, request.Type);
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(JsonSerializer.Serialize(result.response, GlobalSettings.JsonOptions));
    }

    private void HandleGetKeyVaultRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string keyVaultName)
    {
        var vault = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName);
        if (vault.Result == OperationResult.NotFound || vault.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(vault.Resource.ToString());
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
        if (result.Result != OperationResult.Created || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }
        
        response.StatusCode = result.Result == OperationResult.Created ? HttpStatusCode.Created : HttpStatusCode.OK;
        response.Content = new StringContent(result.Resource.ToString());
    }
    
    private void HandleListSubscriptionResourcesRequest(SubscriptionIdentifier subscriptionId, string? filter, HttpResponseMessage response)
    {
        logger.LogDebug($"Executing {nameof(HandleListSubscriptionResourcesRequest)}: Attempting to list resources for subscription ID `{subscriptionId}` and filter `{filter}`.");

        var keyVaults = _controlPlane.ListBySubscription(subscriptionId);
        if (keyVaults.result != OperationResult.Success || keyVaults.resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = new ListSubscriptionResourcesResponse
        {
            Value = keyVaults.resource.Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From!).ToArray()
        };
        
        response.Content = new StringContent(result.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }
}