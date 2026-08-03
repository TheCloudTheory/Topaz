using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement;

internal sealed class ApiManagementBackendControlPlane(
    Pipeline eventPipeline,
    ApiManagementResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    public static ApiManagementBackendControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new ApiManagementResourceProvider(logger), logger);
    
    private static readonly string BackendSubresourceId = nameof(Subresources.Backends).ToLowerInvariant();
    private static readonly string BackendEtagSubresourceId = "backends-etag";

    private readonly ApiManagementServiceControlPlane _apiManagementServiceControlPlane =
        ApiManagementServiceControlPlane.New(eventPipeline, logger);
    
    public OperationResult Deploy(GenericResource resource)
    {
        throw new NotImplementedException();
    }
    
    public ControlPlaneOperationResult<BackendContractResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        string backendId, CreateOrUpdateBackendRequest request, string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, backendId);
        (bool IsValid, string? Error) validationResult;
        if (existing.Result == OperationResult.NotFound)
        {
            var api = new BackendContractResource(subscriptionIdentifier, resourceGroupIdentifier, apimName, backendId,
                BackendContractResourceProperties.From(request));
            
            validationResult = api.Validate<ApiContractResource>();
            if (!validationResult.IsValid)
            {
                return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.BadRequest, null,
                    validationResult.Error, "InvalidRequest");
            }
            
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName,
                BackendSubresourceId, api);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName,
                BackendEtagSubresourceId, api.ETag);

            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.Created, api);
        }

        // As per API docs, If-Match is required for CreateOrUpdate operation
        // when it's an update operation
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }
        
        existing.Resource!.UpdateFromRequest(request);
        validationResult = existing.Resource!.Validate<ApiContractResource>();
        if (!validationResult.IsValid)
        {
            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.BadRequest, null,
                validationResult.Error, "InvalidRequest");
        }

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName,
            BackendEtagSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, backendId, apimName,
            BackendEtagSubresourceId, existing.Resource.ETag);

        return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.Updated, existing.Resource);
    }

    public ControlPlaneOperationResult<BackendContractResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        string backendId)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.GetSubresourceAs<BackendContractResource>(subscriptionIdentifier, resourceGroupIdentifier,
            backendId, apimName, BackendEtagSubresourceId);

        if (existing == null)
        {
            return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.NotFound, null,
                $"Backend {backendId} not found", "ApiNotFound");
        }
        
        return new ControlPlaneOperationResult<BackendContractResource>(OperationResult.Success, existing);
    }
}