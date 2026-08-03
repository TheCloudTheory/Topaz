using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement;

internal sealed class ApiManagementProductControlPlane(Pipeline eventPipeline, ApiManagementResourceProvider provider, ITopazLogger logger) : IControlPlane
{
    private static readonly string ProductSubresourceId = nameof(Subresources.Products).ToLowerInvariant();
    private static readonly string ProductEtagSubresourceId = "products-etag";
    private static readonly string ApiSubscriptionSubresourceId = "products-subscriptions";
    private static readonly string ProductApiAssignmentSubresourceId = nameof(Subresources.ProductApiAssignment).ToLowerInvariant();
    
    public static ApiManagementProductControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new ApiManagementResourceProvider(logger), logger);
    
    private readonly ApiManagementServiceControlPlane _apiManagementServiceControlPlane =
        ApiManagementServiceControlPlane.New(eventPipeline, logger);
    
    private readonly ApiManagementApiControlPlane _apiControlPlane =
        ApiManagementApiControlPlane.New(eventPipeline, logger);
    
    public OperationResult Deploy(GenericResource resource)
    {
        throw new NotImplementedException();
    }

    public ControlPlaneOperationResult<ProductContractResource> CreateOrUpdate(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string apimName, string productId,
        CreateOrUpdateProductRequest request, string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ProductContractResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, productId);
        (bool IsValid, string? Error) validationResult;
        if (existing.Result == OperationResult.NotFound)
        {
            var product = new ProductContractResource(subscriptionIdentifier, resourceGroupIdentifier, apimName, productId,
                ProductContractResourceProperties.From(request));
            
            validationResult = product.Validate<ApiContractResource>();
            if (!validationResult.IsValid)
            {
                return new ControlPlaneOperationResult<ProductContractResource>(OperationResult.BadRequest, null,
                    validationResult.Error, "InvalidRequest");
            }
            
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, productId, apimName,
                ProductSubresourceId, product);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, productId, apimName,
                ProductEtagSubresourceId, product.ETag);

            return new ControlPlaneOperationResult<ProductContractResource>(OperationResult.Created, product);
        }

        // As per API docs, If-Match is required for CreateOrUpdate operation
        // when it's an update operation
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<ProductContractResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }
        
        existing.Resource!.UpdateFromRequest(request);
        validationResult = existing.Resource!.Validate<ProductContractResource>();
        if (!validationResult.IsValid)
        {
            return new ControlPlaneOperationResult<ProductContractResource>(OperationResult.BadRequest, null,
                validationResult.Error, "InvalidRequest");
        }

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, productId, apimName,
            ProductSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, productId, apimName,
            ProductEtagSubresourceId, existing.Resource.ETag);

        return new ControlPlaneOperationResult<ProductContractResource>(OperationResult.Updated, existing.Resource);
    }
    
    public ControlPlaneOperationResult<ProductContractResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        string productId)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ProductContractResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.GetSubresourceAs<ProductContractResource>(subscriptionIdentifier, resourceGroupIdentifier,
            productId, apimName, ProductSubresourceId);

        if (existing == null)
        {
            return new ControlPlaneOperationResult<ProductContractResource>(OperationResult.NotFound, null,
                $"Product {productId} not found", "ProductNotFound");
        }
        
        return new ControlPlaneOperationResult<ProductContractResource>(OperationResult.Success, existing);
    }
    
    public ControlPlaneOperationResult Delete(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string apimName, string productId, string? ifMatch,
        bool deleteSubscriptions)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound,
                apimOperation.Reason, apimOperation.Code);
        }
        
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, productId);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, existing.Reason, existing.Code);
        }
        
        // As per docs, If-Match header must be present for delete operation
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult(OperationResult.BadRequest,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }
        
        var etag = provider.GetSubresourceAs<ContractEtag>(subscriptionIdentifier, resourceGroupIdentifier, productId,
            apimName, ProductEtagSubresourceId);

        if (etag == null)
        {
            logger.LogError(nameof(ApiManagementApiControlPlane), nameof(Delete), "API Management API is missing ETag value");
            
            return new ControlPlaneOperationResult(OperationResult.Failed, "ETag not found",
                "InvalidStateError");
        }

        if (ifMatch != "*" && !etag.IsEqualToETag(ifMatch))
        {
            return new ControlPlaneOperationResult(OperationResult.Conflict,
                "If-Match does not match ETag value", "ConcurrentOperationFailed");
        }
        
        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, productId, apimName, ProductSubresourceId);
        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, productId, apimName, ProductEtagSubresourceId);

        if (!deleteSubscriptions) return new ControlPlaneOperationResult(OperationResult.Deleted);
        
        logger.LogDebug(nameof(ApiManagementApiControlPlane), nameof(Delete), "Deleting all revisions.");
        
        var subscriptionsToDelete = provider.ListSubresourcesAs<SubscriptionContractResource>(subscriptionIdentifier,
            resourceGroupIdentifier, apimName, ApiSubscriptionSubresourceId).Where(subscription =>
            subscription.Id.Contains(productId));

        foreach (var subscription in subscriptionsToDelete)
        {
            provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, subscription.GetOwnerId(),
                apimName, ApiSubscriptionSubresourceId);
        }

        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }
    
    public ControlPlaneOperationResult<string> GetEntityTag(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName, string productId)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<string>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, productId);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<string>(OperationResult.NotFound, null, existing.Reason, existing.Code);
        }

        var etag = provider.GetSubresourceAs<ContractEtag>(subscriptionIdentifier, resourceGroupIdentifier, productId,
            apimName, ProductEtagSubresourceId);

        return new ControlPlaneOperationResult<string>(OperationResult.Success, etag?.Value);
    }
    
    public ControlPlaneOperationResult<ProductContractResource> Update(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string apimName, string productId, CreateOrUpdateProductRequest request,
        string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ProductContractResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, productId);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ProductContractResource>(OperationResult.NotFound, null, existing.Reason, existing.Code);
        }

        // As per API docs, If-Match is required for Update operation,
        // and it must match the current ETag (unless it's unconditional update with "*")
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<ProductContractResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }

        var etag = provider.GetSubresourceAs<ContractEtag>(subscriptionIdentifier, resourceGroupIdentifier, productId,
            apimName, ProductEtagSubresourceId);

        if (etag == null)
        {
            logger.LogError(nameof(ApiManagementApiControlPlane), nameof(Update), "API Management API is missing ETag value");
            
            return new ControlPlaneOperationResult<ProductContractResource>(OperationResult.Failed, null, "ETag not found",
                "InvalidStateError");
        }

        if (ifMatch != "*" && !etag.IsEqualToETag(ifMatch))
        {
            return new ControlPlaneOperationResult<ProductContractResource>(OperationResult.Conflict, null,
                "If-Match does not match ETag value", "ConcurrentOperationFailed");
        }
        
        existing.Resource!.UpdateFromRequest(request);
        var validationResult = existing.Resource!.Validate<ApiContractResource>();
        if (!validationResult.IsValid)
        {
            return new ControlPlaneOperationResult<ProductContractResource>(OperationResult.BadRequest, null,
                validationResult.Error, "InvalidRequest");
        }

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, productId, apimName,
            ProductSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, productId, apimName,
            ProductEtagSubresourceId, existing.Resource.ETag);

        return new ControlPlaneOperationResult<ProductContractResource>(OperationResult.Updated, existing.Resource);
    }

    public ControlPlaneOperationResult<ProductContractResource[]> ListByService(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ProductContractResource[]>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.ListSubresourcesAs<ProductContractResource>(subscriptionIdentifier,
            resourceGroupIdentifier,
            apimName, ProductSubresourceId);

        return new ControlPlaneOperationResult<ProductContractResource[]>(OperationResult.Success, existing);
    }

    public ControlPlaneOperationResult<ApiContractResource> CreateOrUpdateProductApi(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName, string productId, string apiId)
    {
        var productOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, productId);
        if (productOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.NotFound, null,
                productOperation.Reason, productOperation.Code);
        }
        
        var apiOperation = _apiControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, apiId);
        if (apiOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.NotFound, null,
                apiOperation.Reason, apiOperation.Code);
        }
        
        var existing = provider.GetSubresourceAs<ProductApiAssignment>(subscriptionIdentifier,
            resourceGroupIdentifier,
            ProductApiAssignment.GetId(productId, apiId),
            apimName, ProductApiAssignmentSubresourceId);

        if (existing != null)
        {
            var assignment = ProductApiAssignment
                .New(existing.ApiId!, existing.ProductId!, apimName);
            
            provider.CreateOrUpdateSubresource(subscriptionIdentifier,
                resourceGroupIdentifier,
                assignment.GetId(),
                apimName, ProductApiAssignmentSubresourceId, assignment);

            return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.Created, apiOperation.Resource);
        }

        existing!.UpdateFrom(productId, apiId);
        
        provider.CreateOrUpdateSubresource(subscriptionIdentifier,
            resourceGroupIdentifier,
            ProductApiAssignment.GetId(productId, apiId),
            apimName, ProductApiAssignmentSubresourceId, existing);

        return new ControlPlaneOperationResult<ApiContractResource>(OperationResult.Updated, apiOperation.Resource);
    }

    public ControlPlaneOperationResult CheckAssignmentExists(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName, string productId, string apiId)
    {
        var productOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, productId);
        if (productOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, productOperation.Reason, productOperation.Code);
        }
        
        var apiOperation = _apiControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, apiId);
        if (apiOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, apiOperation.Reason, apiOperation.Code);
        }
        
        var existing = provider.GetSubresourceAs<ProductApiAssignment>(subscriptionIdentifier,
            resourceGroupIdentifier,
            ProductApiAssignment.GetId(productId, apiId),
            apimName, ProductApiAssignmentSubresourceId);

        return existing == null
            ? new ControlPlaneOperationResult(OperationResult.NotFound, "Product API assignment not found",
                "ProductApiAssignmentNotFound")
            : new ControlPlaneOperationResult(OperationResult.Success);
    }

    public ControlPlaneOperationResult DeleteProductApi(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName, string productId, string apiId)
    {
        var productOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, productId);
        if (productOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, productOperation.Reason, productOperation.Code);
        }
        
        var apiOperation = _apiControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, apiId);
        if (apiOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, apiOperation.Reason, apiOperation.Code);
        }
        
        var existing = provider.GetSubresourceAs<ProductApiAssignment>(subscriptionIdentifier,
            resourceGroupIdentifier,
            ProductApiAssignment.GetId(productId, apiId),
            apimName, ProductApiAssignmentSubresourceId);

        if (existing == null)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, "Product API assignment not found",
                "ProductApiAssignmentNotFound");
        }
        
        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, ProductApiAssignment.GetId(productId, apiId), apimName, ProductApiAssignmentSubresourceId);
        
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<ApiContractResource[]> ListByProduct(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName, string productId)
    {
        var productOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, apimName, productId);
        if (productOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiContractResource[]>(OperationResult.NotFound, null, productOperation.Reason, productOperation.Code);
        }
        
        var apisOperation = _apiControlPlane.ListByService(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apisOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<ApiContractResource[]>(OperationResult.NotFound, null, apisOperation.Reason, apisOperation.Code);
        }

        var assignments = provider
            .ListSubresourcesAs<ProductApiAssignment>(subscriptionIdentifier, resourceGroupIdentifier, apimName,
                ProductApiAssignmentSubresourceId).Where(assignment => assignment.ProductId == productId);
        
        var apis = apisOperation.Resource!.Where(api => assignments.Any(assignment => api.Id.Contains(assignment.ApiId!))).ToArray();
        return new ControlPlaneOperationResult<ApiContractResource[]>(OperationResult.Success, apis);
    }
}