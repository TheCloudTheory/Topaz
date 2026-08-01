using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement;

internal sealed class ApiManagementApiControlPlane(
    Pipeline eventPipeline,
    ApiManagementResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    public static ApiManagementApiControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new ApiManagementResourceProvider(logger), logger);

    private static readonly string ApiSubresourceId = nameof(Subresources.Apis).ToLowerInvariant();

    private readonly ApiManagementServiceControlPlane _apiManagementServiceControlPlane =
        ApiManagementServiceControlPlane.New(eventPipeline, logger);

    public OperationResult Deploy(GenericResource resource)
    {
        throw new NotImplementedException();
    }

    public ControlPlaneOperationResult<ApiContractResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        string apiId, CreateOrUpdateApiRequest request, string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, apiId);
        (bool IsValid, string? Error) validationResult;
        if (existing.Result == OperationResult.NotFound)
        {
            var api = new ApiContractResource(subscriptionIdentifier, resourceGroupIdentifier, apimName, apiId,
                ApiContractResourceProperties.From(request));
            
            validationResult = api.Validate<ApiContractResource>();
            if (!validationResult.IsValid)
            {
                return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.BadRequest, null,
                    validationResult.Error, "InvalidRequest");
            }
            
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, apiId, apimName,
                ApiSubresourceId, request);

            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.Success, null);
        }

        // As per API docs, If-Match is required for CreateOrUpdate operation
        // when it's an update operation
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }
        
        existing.Resource!.UpdateFromRequest(request);
        validationResult = existing.Resource!.Validate<ApiContractResource>();
        if (!validationResult.IsValid)
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.BadRequest, null,
                validationResult.Error, "InvalidRequest");
        }

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, apiId, apimName,
            ApiSubresourceId, request);

        return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.Success, null);
    }

    public ControlPlaneOperationResult<ApiContractResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        string apiId)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.GetSubresourceAs<ApiContractResource>(subscriptionIdentifier, resourceGroupIdentifier,
            apiId, apimName, ApiSubresourceId);

        if (existing == null)
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.NotFound, null,
                $"API {apiId} not found", "ApiNotFound");
        }
        
        return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.Success, existing);
    }

}