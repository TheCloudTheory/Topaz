using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement;

internal sealed class PortalSettingsControlPlane(
    Pipeline eventPipeline,
    ApiManagementResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    public static PortalSettingsControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new ApiManagementResourceProvider(logger), logger);
    
    private static readonly string PortalSettingsSubresourceId = nameof(Subresources.PortalSettings).ToLowerInvariant();
    private static readonly string PortalSettingsETagSubresourceId = "portalsettings-etag";
    
    private readonly ApiManagementServiceControlPlane _apiManagementServiceControlPlane =
        ApiManagementServiceControlPlane.New(eventPipeline, logger);
    
    public OperationResult Deploy(GenericResource resource)
    {
        throw new NotImplementedException();
    }
    
    public ControlPlaneOperationResult<PortalSignInSettingsResource> GetSignInSettings(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.GetSubresourceAs<PortalSignInSettingsResource>(subscriptionIdentifier,
            resourceGroupIdentifier, "signin", apimName, PortalSettingsSubresourceId);

        return existing != null
            ? new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.Success, existing)
            : new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.NotFound, null,
                "SignInSettings not found", "PortalSettingsNotFound");
    }

    public ControlPlaneOperationResult<PortalSignInSettingsResource> CreateOrUpdateSignInSettings(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        CreateOrUpdatePortalSignInSettingsRequest request, string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = GetSignInSettings(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (existing.Result == OperationResult.NotFound)
        {
            var signInSettings = new PortalSignInSettingsResource(subscriptionIdentifier, resourceGroupIdentifier, apimName,
                PortalSignInSettingsResource.From(request));

            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, "signin", apimName,
                PortalSettingsSubresourceId, signInSettings);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, "signin", apimName,
                PortalSettingsETagSubresourceId, signInSettings.ETag);

            return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.Created, signInSettings);
        }

        // As per API docs, If-Match is required for CreateOrUpdateSignInSettings operation
        // when it's an update operation
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }

        existing.Resource!.UpdateFromRequest(request);

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, "signin", apimName,
            PortalSettingsSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, "signin", apimName,
            PortalSettingsETagSubresourceId, existing.Resource.ETag);

        return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.Updated, existing.Resource);
    }

    public ControlPlaneOperationResult<string> GetSignInSettingsEntityTag(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<string>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = GetSignInSettings(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<string>(OperationResult.NotFound, null, existing.Reason, existing.Code);
        }

        var etag = provider.GetSubresourceAs<ContractEtag>(subscriptionIdentifier, resourceGroupIdentifier, "signin",
            apimName, PortalSettingsETagSubresourceId);

        return new ControlPlaneOperationResult<string>(OperationResult.Success, etag?.Value);
    }

    public ControlPlaneOperationResult<PortalSignInSettingsResource> Update(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        CreateOrUpdatePortalSignInSettingsRequest request, string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = GetSignInSettings(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.NotFound, null,
                existing.Reason, existing.Code);
        }

        // As per API docs, If-Match is required for Update operation,
        // and it must match the current ETag (unless it's unconditional update with "*")
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }

        var etag = provider.GetSubresourceAs<ContractEtag>(subscriptionIdentifier, resourceGroupIdentifier, "signin",
            apimName, PortalSettingsETagSubresourceId);

        if (etag == null)
        {
            logger.LogError(nameof(ApiManagementApiControlPlane), nameof(Update),
                "API Management sign-in setting is missing ETag value");

            return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.Failed, null,
                "ETag not found",
                "InvalidStateError");
        }

        if (ifMatch != "*" && !etag.IsEqualToETag(ifMatch))
        {
            return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.Conflict, null,
                "If-Match does not match ETag value", "ConcurrentOperationFailed");
        }

        existing.Resource!.UpdateFromRequest(request);

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, "signin", apimName,
            PortalSettingsSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, "signin", apimName,
            PortalSettingsETagSubresourceId, existing.Resource.ETag);

        return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.Updated, existing.Resource);
    }

    public ControlPlaneOperationResult<PortalSignUpSettingsResource> CreateOrUpdateSignUpSettings(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        CreateOrUpdatePortalSignUpSettingsRequest request, string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PortalSignUpSettingsResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = GetSignUpSettings(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (existing.Result == OperationResult.NotFound)
        {
            var signInSettings = new PortalSignUpSettingsResource(subscriptionIdentifier, resourceGroupIdentifier, apimName,
                PortalSignUpSettingsResource.From(request));

            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, "signup", apimName,
                PortalSettingsSubresourceId, signInSettings);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, "signup", apimName,
                PortalSettingsETagSubresourceId, signInSettings.ETag);

            return new ControlPlaneOperationResult<PortalSignUpSettingsResource>(OperationResult.Created, signInSettings);
        }

        // As per API docs, If-Match is required for CreateOrUpdateSignInSettings operation
        // when it's an update operation
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<PortalSignUpSettingsResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }

        existing.Resource!.UpdateFromRequest(request);

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, "signup", apimName,
            PortalSettingsSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, "signup", apimName,
            PortalSettingsETagSubresourceId, existing.Resource.ETag);

        return new ControlPlaneOperationResult<PortalSignUpSettingsResource>(OperationResult.Updated, existing.Resource);
    }
    
    public ControlPlaneOperationResult<PortalSignUpSettingsResource> GetSignUpSettings(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PortalSignUpSettingsResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.GetSubresourceAs<PortalSignUpSettingsResource>(subscriptionIdentifier,
            resourceGroupIdentifier, "signup", apimName, PortalSettingsSubresourceId);

        return existing != null
            ? new ControlPlaneOperationResult<PortalSignUpSettingsResource>(OperationResult.Success, existing)
            : new ControlPlaneOperationResult<PortalSignUpSettingsResource>(OperationResult.NotFound, null,
                "SignInSettings not found", "PortalSettingsNotFound");
    }
}