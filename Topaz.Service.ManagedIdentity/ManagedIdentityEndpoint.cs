using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ManagedIdentity.Models.Requests;
using Topaz.Service.ManagedIdentity.Models.Responses;
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
                        ManagedIdentityIdentifier.From(managedIdentityName), input);
                    break;
                case "GET":
                    if (resourceGroupIdentifier == null && string.IsNullOrEmpty(managedIdentityName))
                    {
                        HandleListBySubscriptionRequest(response, subscriptionIdentifier);
                        break;
                    }

                    if (string.IsNullOrEmpty(managedIdentityName))
                    {
                        HandleListByResourceGroupRequest(response, subscriptionIdentifier, resourceGroupIdentifier!);
                        break;
                    }

                    HandleGetManagedIdentityRequest(response, subscriptionIdentifier, resourceGroupIdentifier!,
                        ManagedIdentityIdentifier.From(managedIdentityName));
                    break;
                case "DELETE":
                    HandleDeleteManagedIdentityRequest(response, subscriptionIdentifier, resourceGroupIdentifier!, ManagedIdentityIdentifier.From(managedIdentityName));
                    break;
                case "PATCH":
                    HandleUpdateManagedIdentityRequest(response, subscriptionIdentifier, resourceGroupIdentifier!, ManagedIdentityIdentifier.From(managedIdentityName), input);
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

    private void HandleUpdateManagedIdentityRequest(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ManagedIdentityIdentifier managedIdentityIdentifier, Stream input)
    {
        logger.LogDebug($"Executing {nameof(HandleUpdateManagedIdentityRequest)}.");

        using var reader = new StreamReader(input);

        var content = reader.ReadToEnd();
        var request =
            JsonSerializer.Deserialize<CreateUpdateManagedIdentityRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        var result = _controlPlane.Update(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityIdentifier, request);
        if (result.Result != OperationResult.Updated || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(result.Resource.ToString());
    }

    private void HandleDeleteManagedIdentityRequest(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ManagedIdentityIdentifier managedIdentityIdentifier)
    {
        logger.LogDebug($"Executing {nameof(HandleDeleteManagedIdentityRequest)}.");
        
        var managedIdentity = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityIdentifier);
        switch (managedIdentity.Result)
        {
            case OperationResult.NotFound:
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            case OperationResult.Failed:
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            default:
                var result = _controlPlane.Delete(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityIdentifier);
                response.StatusCode = result.Result switch
                {
                    OperationResult.Deleted => HttpStatusCode.OK,
                    OperationResult.NotFound => HttpStatusCode.NotFound,
                    _ => HttpStatusCode.InternalServerError
                };

                break;
        }
    }

    private void HandleGetManagedIdentityRequest(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ManagedIdentityIdentifier managedIdentityIdentifier)
    {
        logger.LogDebug($"Executing {nameof(HandleGetManagedIdentityRequest)}.");

        var operation = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityIdentifier);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource.ToString());
    }

    private void HandleListByResourceGroupRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        logger.LogDebug($"Executing {nameof(HandleListByResourceGroupRequest)}.");
        
        var identities = _controlPlane.ListByResourceGroup(subscriptionIdentifier, resourceGroupIdentifier);
        if (identities.Result != OperationResult.Success || identities.Resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = new UserAssignedIdentitiesListResponse
        {
            Value = identities.Resource.ToArray()
        };
        
        response.Content = new StringContent(result.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleListBySubscriptionRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier)
    {
        logger.LogDebug($"Executing {nameof(HandleListBySubscriptionRequest)}.");
        
        var identities = _controlPlane.ListBySubscription(subscriptionIdentifier);
        if (identities.Result != OperationResult.Success || identities.Resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = new UserAssignedIdentitiesListResponse
        {
            Value = identities.Resource.ToArray()
        };
        
        response.Content = new StringContent(result.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleCreateUpdateManagedIdentityRequest(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ManagedIdentityIdentifier managedIdentityIdentifier, Stream input)
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

        var result = _controlPlane.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, managedIdentityIdentifier, request);
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