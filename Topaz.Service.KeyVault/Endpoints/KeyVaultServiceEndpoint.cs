using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.KeyVault.Models.Responses;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Endpoints;

internal sealed class KeyVaultServiceEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultControlPlane _controlPlane = new(
        new KeyVaultResourceProvider(logger),
        new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger),
            new SubscriptionControlPlane(new SubscriptionResourceProvider(logger)), logger),
        new SubscriptionControlPlane(new SubscriptionResourceProvider(logger)), logger);
    
    public string[] Endpoints => [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}",
        "GET /subscriptions/{subscriptionId}/resources",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults",
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.KeyVault/deletedVaults",
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.KeyVault/deletedManagedHSMs",
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.KeyVault/locations/{location}/deletedVaults/{keyVaultName}",
        "POST /subscriptions/{subscriptionId}/providers/Microsoft.KeyVault/checkNameAvailability",
        "POST /subscriptions/{subscriptionId}/providers/Microsoft.KeyVault/checkMhsmNameAvailability",
        "POST /subscriptions/{subscriptionId}/providers/Microsoft.KeyVault/locations/{location}/deletedVaults/{keyVaultName}/purge",
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}",
        "PATCH /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}"
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
                        // TODO: Separate logic for returning different kinds of deleted Key Vaults
                        if (path.EndsWith("deletedVaults", StringComparison.OrdinalIgnoreCase) || path.EndsWith("deletedManagedHSMs", StringComparison.OrdinalIgnoreCase))
                        {
                            HandleListDeletedVaultsRequest(response, subscriptionIdentifier);
                            break;
                        }

                        var isShowDeletedRequest = path.ExtractValueFromPath(7) == "deletedVaults";
                        if (isShowDeletedRequest)
                        {
                            HandleShowDeletedVaultRequest(response, subscriptionIdentifier, keyVaultName!);
                            break;
                        }
                        
                        if (string.IsNullOrWhiteSpace(keyVaultName))
                        {
                            HandleListKeyVaultsByResourceGroupRequest(response, subscriptionIdentifier, ResourceGroupIdentifier.From(resourceGroupSegment));
                            break;
                        }
                        
                        HandleGetKeyVaultRequest(response, subscriptionIdentifier, ResourceGroupIdentifier.From(resourceGroupSegment), keyVaultName);
                    }
                    
                    break;
                case "POST":
                    if (string.IsNullOrWhiteSpace(keyVaultName))
                    {
                        HandleCheckNameRequest(response, subscriptionIdentifier, input);
                        break;
                    }
                    
                    var location = path.ExtractValueFromPath(6);
                    HandlePurgeKeyVaultRequest(response, subscriptionIdentifier, location!, keyVaultName);
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
                case "PATCH":
                    HandleUpdateKeyVaultRequest(response, subscriptionIdentifier, ResourceGroupIdentifier.From(resourceGroupSegment), keyVaultName!, input);
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

    private void HandleUpdateKeyVaultRequest(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string keyVaultName, Stream input)
    {
        logger.LogDebug($"Executing {nameof(HandleCreateUpdateKeyVaultRequest)}.");
        
        using var reader = new StreamReader(input);

        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<UpdateKeyVaultRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        var result = _controlPlane.Update(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName, request);
        if (result.Result != OperationResult.Updated || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(result.Resource.ToString());
    }

    private void HandlePurgeKeyVaultRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, string location, string keyVaultName)
    {
        logger.LogDebug($"Executing {nameof(HandlePurgeKeyVaultRequest)} for `{subscriptionIdentifier}` and `{keyVaultName}` and `{location}`.");
        
        var (operationResult, vaultUri) = _controlPlane.Purge(subscriptionIdentifier, location, keyVaultName);
        if (operationResult == OperationResult.NotFound || vaultUri == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.Headers.Location = new Uri(vaultUri);
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleShowDeletedVaultRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, string keyVaultName)
    {
        logger.LogDebug($"Executing {nameof(HandleListDeletedVaultsRequest)} for `{subscriptionIdentifier}` and `{keyVaultName}`.");
        
        var (operationResult, keyVault) = _controlPlane.ShowDeleted(subscriptionIdentifier, keyVaultName);
        if (operationResult == OperationResult.NotFound || keyVault == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var result = new ListDeletedResponse.DeletedKeyVaultResponse
        {
            Id =
                $"/subscriptions/{keyVault!.GetSubscription().Value}/providers/Microsoft.KeyVault/locations/{keyVault.Location}/deletedVaults/{keyVault.Name}",
            Name = keyVault.Name,
            Properties = new ListDeletedResponse.DeletedKeyVaultResponse.DeletedKeyVaultProperties
            {
                VaultId = keyVault.Id,
                Location = keyVault.Location,
                DeletionDate = keyVault.DeletionDate,
                ScheduledPurgeDate =  keyVault.ScheduledPurgeDate
            }
        };
        
        response.Content = new StringContent(result.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleListDeletedVaultsRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier)
    {
        logger.LogDebug($"Executing {nameof(HandleListDeletedVaultsRequest)}.");
        
        var keyVaults = _controlPlane.ListDeletedBySubscription(subscriptionIdentifier);
        if (keyVaults.result != OperationResult.Success || keyVaults.resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = new ListDeletedResponse
        {
            Value = keyVaults.resource.Select(keyVault => new ListDeletedResponse.DeletedKeyVaultResponse
            {
                Id =
                    $"/subscriptions/{keyVault!.GetSubscription().Value}/providers/Microsoft.KeyVault/locations/{keyVault.Location}/deletedVaults/{keyVault.Name}",
                Name = keyVault.Name,
                Properties = new ListDeletedResponse.DeletedKeyVaultResponse.DeletedKeyVaultProperties
                {
                    VaultId = keyVault.Id,
                    Location = keyVault.Location,
                    DeletionDate =  keyVault.DeletionDate,
                    ScheduledPurgeDate =   keyVault.ScheduledPurgeDate
                }
            }).ToArray()
        };
        
        response.Content = new StringContent(result.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleListKeyVaultsByResourceGroupRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        logger.LogDebug($"Executing {nameof(HandleListKeyVaultsByResourceGroupRequest)}.");
        
        var keyVaults = _controlPlane.ListByResourceGroup(subscriptionIdentifier, resourceGroupIdentifier);
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

    private void HandleDeleteKeyVaultRequest(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string keyVaultName)
    {
        logger.LogDebug($"Executing {nameof(HandleDeleteKeyVaultRequest)}.");
        
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
        logger.LogDebug($"Executing {nameof(HandleCheckNameRequest)}.");
        
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
        logger.LogDebug($"Executing {nameof(HandleGetKeyVaultRequest)}.");
        
        var operation = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource.ToString());
    }

    private void HandleCreateUpdateKeyVaultRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string keyVaultName, Stream input)
    {
        logger.LogDebug($"Executing {nameof(HandleCreateUpdateKeyVaultRequest)}.");
        
        using var reader = new StreamReader(input);

        var content = reader.ReadToEnd();
        logger.LogDebug(nameof(HandleCreateUpdateKeyVaultRequest), $"Processing payload: {content}");
        
        var request = JsonSerializer.Deserialize<CreateOrUpdateKeyVaultRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = _controlPlane.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName, request);
        if ((result.Result != OperationResult.Created && result.Result != OperationResult.Updated) || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }
        
        response.StatusCode = result.Result == OperationResult.Created ? HttpStatusCode.Created : HttpStatusCode.OK;
        response.Content = new StringContent(result.Resource.ToString());
    }
    
    private void HandleListSubscriptionResourcesRequest(SubscriptionIdentifier subscriptionIdentifier, string? filter, HttpResponseMessage response)
    {
        logger.LogDebug($"Executing {nameof(HandleListSubscriptionResourcesRequest)}: Attempting to list resources for subscription ID `{subscriptionIdentifier}` and filter `{filter}`.");

        var keyVaults = _controlPlane.ListBySubscription(subscriptionIdentifier);
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