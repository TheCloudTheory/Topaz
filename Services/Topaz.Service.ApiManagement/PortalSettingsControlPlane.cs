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
    private const string SignInSettingsId = "signin";
    private const string SignUpSettingsId = "signup";
    private const string DelegationSettingsId = "delegation";
    
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

    public ControlPlaneOperationResult<PortalSignInSettingsResource> GetSignInSettings(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.GetSubresourceAs<PortalSignInSettingsResource>(subscriptionIdentifier,
            resourceGroupIdentifier, SignInSettingsId, apimName, PortalSettingsSubresourceId);

        return existing != null
            ? new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.Success, existing)
            : new ControlPlaneOperationResult<PortalSignInSettingsResource>(OperationResult.Success, PortalSignInSettingsResource.Default);
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
        if (existing.Result == OperationResult.NotFound || existing.Resource?.IsDefault == true)
        {
            var signInSettings = new PortalSignInSettingsResource(subscriptionIdentifier, resourceGroupIdentifier, apimName,
                PortalSignInSettingsResource.From(request));

            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, SignInSettingsId, apimName,
                PortalSettingsSubresourceId, signInSettings);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, SignInSettingsId, apimName,
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

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, SignInSettingsId, apimName,
            PortalSettingsSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, SignInSettingsId, apimName,
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

        var etag = provider.GetSubresourceAs<ContractEtag>(subscriptionIdentifier, resourceGroupIdentifier, SignInSettingsId,
            apimName, PortalSettingsETagSubresourceId);

        return new ControlPlaneOperationResult<string>(OperationResult.Success, etag?.Value);
    }

    public ControlPlaneOperationResult<PortalSignInSettingsResource> UpdateSignInSettings(
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

        var etag = provider.GetSubresourceAs<ContractEtag>(subscriptionIdentifier, resourceGroupIdentifier, SignInSettingsId,
            apimName, PortalSettingsETagSubresourceId);

        if (etag == null)
        {
            logger.LogError(nameof(ApiManagementApiControlPlane), nameof(UpdateSignInSettings),
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

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, SignInSettingsId, apimName,
            PortalSettingsSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, SignInSettingsId, apimName,
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
        if (existing.Result == OperationResult.NotFound || existing.Resource?.IsDefault == true)
        {
            var signInSettings = new PortalSignUpSettingsResource(subscriptionIdentifier, resourceGroupIdentifier, apimName,
                PortalSignUpSettingsResource.From(request));

            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, SignUpSettingsId, apimName,
                PortalSettingsSubresourceId, signInSettings);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, SignUpSettingsId, apimName,
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

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, SignUpSettingsId, apimName,
            PortalSettingsSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, SignUpSettingsId, apimName,
            PortalSettingsETagSubresourceId, existing.Resource.ETag);

        return new ControlPlaneOperationResult<PortalSignUpSettingsResource>(OperationResult.Updated, existing.Resource);
    }

    public ControlPlaneOperationResult<PortalSignUpSettingsResource> GetSignUpSettings(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PortalSignUpSettingsResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.GetSubresourceAs<PortalSignUpSettingsResource>(subscriptionIdentifier,
            resourceGroupIdentifier, SignUpSettingsId, apimName, PortalSettingsSubresourceId);

        return existing != null
            ? new ControlPlaneOperationResult<PortalSignUpSettingsResource>(OperationResult.Success, existing)
            : new ControlPlaneOperationResult<PortalSignUpSettingsResource>(OperationResult.Success, PortalSignUpSettingsResource.Default);
    }

    public ControlPlaneOperationResult<string> GetSignUpSettingsEntityTag(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string apimName)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<string>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = GetSignUpSettings(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<string>(OperationResult.NotFound, null, existing.Reason,
                existing.Code);
        }

        var etag = provider.GetSubresourceAs<ContractEtag>(subscriptionIdentifier, resourceGroupIdentifier,
            SignUpSettingsId,
            apimName, PortalSettingsETagSubresourceId);

        return new ControlPlaneOperationResult<string>(OperationResult.Success, etag?.Value);
    }

    public ControlPlaneOperationResult<PortalSignUpSettingsResource> UpdateSignUpSettings(
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
            return new ControlPlaneOperationResult<PortalSignUpSettingsResource>(OperationResult.NotFound, null,
                existing.Reason, existing.Code);
        }

        // As per API docs, If-Match is required for Update operation,
        // and it must match the current ETag (unless it's unconditional update with "*")
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<PortalSignUpSettingsResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }

        var etag = provider.GetSubresourceAs<ContractEtag>(subscriptionIdentifier, resourceGroupIdentifier, SignUpSettingsId,
            apimName, PortalSettingsETagSubresourceId);

        if (etag == null)
        {
            logger.LogError(nameof(ApiManagementApiControlPlane), nameof(UpdateSignInSettings),
                "API Management sign-up setting is missing ETag value");

            return new ControlPlaneOperationResult<PortalSignUpSettingsResource>(OperationResult.Failed, null,
                "ETag not found",
                "InvalidStateError");
        }

        if (ifMatch != "*" && !etag.IsEqualToETag(ifMatch))
        {
            return new ControlPlaneOperationResult<PortalSignUpSettingsResource>(OperationResult.Conflict, null,
                "If-Match does not match ETag value", "ConcurrentOperationFailed");
        }

        existing.Resource!.UpdateFromRequest(request);

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, SignUpSettingsId, apimName,
            PortalSettingsSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, SignUpSettingsId, apimName,
            PortalSettingsETagSubresourceId, existing.Resource.ETag);

        return new ControlPlaneOperationResult<PortalSignUpSettingsResource>(OperationResult.Updated, existing.Resource);
    }
    
    public ControlPlaneOperationResult<PortalDelegationSettingsResource> CreateOrUpdateDelegationSettings(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        CreateOrUpdatePortalDelegationSettingsRequest request, string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PortalDelegationSettingsResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = GetDelegationSettings(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (existing.Result == OperationResult.NotFound || existing.Resource?.IsDefault == true)
        {
            var signInSettings = new PortalDelegationSettingsResource(subscriptionIdentifier, resourceGroupIdentifier, apimName,
                PortalDelegationSettingsResourceProperties.From(request));

            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, DelegationSettingsId, apimName,
                PortalSettingsSubresourceId, signInSettings);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, DelegationSettingsId, apimName,
                PortalSettingsETagSubresourceId, signInSettings.ETag);

            return new ControlPlaneOperationResult<PortalDelegationSettingsResource>(OperationResult.Created, signInSettings);
        }

        // As per API docs, If-Match is required for CreateOrUpdateSignInSettings operation
        // when it's an update operation
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<PortalDelegationSettingsResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }

        existing.Resource!.UpdateFromRequest(request);

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, DelegationSettingsId, apimName,
            PortalSettingsSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, DelegationSettingsId, apimName,
            PortalSettingsETagSubresourceId, existing.Resource.ETag);

        return new ControlPlaneOperationResult<PortalDelegationSettingsResource>(OperationResult.Updated, existing.Resource);
    }

    public ControlPlaneOperationResult<PortalDelegationSettingsResource> GetDelegationSettings(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PortalDelegationSettingsResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = provider.GetSubresourceAs<PortalDelegationSettingsResource>(subscriptionIdentifier,
            resourceGroupIdentifier, DelegationSettingsId, apimName, PortalSettingsSubresourceId);

        return existing != null
            ? new ControlPlaneOperationResult<PortalDelegationSettingsResource>(OperationResult.Success, existing)
            : new ControlPlaneOperationResult<PortalDelegationSettingsResource>(OperationResult.Success, PortalDelegationSettingsResource.Default);
    }

    public ControlPlaneOperationResult<string> GetDelegationSettingsEntityTag(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string apimName)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<string>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = GetDelegationSettings(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<string>(OperationResult.NotFound, null, existing.Reason,
                existing.Code);
        }

        var etag = provider.GetSubresourceAs<ContractEtag>(subscriptionIdentifier, resourceGroupIdentifier,
            DelegationSettingsId,
            apimName, PortalSettingsETagSubresourceId);

        return new ControlPlaneOperationResult<string>(OperationResult.Success, etag?.Value);
    }

    public ControlPlaneOperationResult<PortalSettingValidationKeyContract> ListDelegationSettingsSecrets(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PortalSettingValidationKeyContract>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = GetDelegationSettings(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PortalSettingValidationKeyContract>(OperationResult.NotFound, null, existing.Reason,
                existing.Code);
        }

        var result = PortalSettingValidationKeyContract.From(existing.Resource);
        return new ControlPlaneOperationResult<PortalSettingValidationKeyContract>(OperationResult.Success, result);
    }
    
    public ControlPlaneOperationResult<PortalDelegationSettingsResource> UpdateDelegationSettings(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string apimName,
        CreateOrUpdatePortalDelegationSettingsRequest request, string? ifMatch)
    {
        var apimOperation =
            _apiManagementServiceControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (apimOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PortalDelegationSettingsResource>(OperationResult.NotFound, null,
                apimOperation.Reason, apimOperation.Code);
        }

        var existing = GetDelegationSettings(subscriptionIdentifier, resourceGroupIdentifier, apimName);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<PortalDelegationSettingsResource>(OperationResult.NotFound, null,
                existing.Reason, existing.Code);
        }

        // As per API docs, If-Match is required for Update operation,
        // and it must match the current ETag (unless it's unconditional update with "*")
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new ControlPlaneOperationResult<PortalDelegationSettingsResource>(OperationResult.BadRequest, null,
                "If-Match is required for update requests.", "MissingIfMatchHeader");
        }

        var etag = provider.GetSubresourceAs<ContractEtag>(subscriptionIdentifier, resourceGroupIdentifier, DelegationSettingsId,
            apimName, PortalSettingsETagSubresourceId);

        if (etag == null)
        {
            logger.LogError(nameof(ApiManagementApiControlPlane), nameof(UpdateDelegationSettings),
                "API Management delegation setting is missing ETag value");

            return new ControlPlaneOperationResult<PortalDelegationSettingsResource>(OperationResult.Failed, null,
                "ETag not found",
                "InvalidStateError");
        }

        if (ifMatch != "*" && !etag.IsEqualToETag(ifMatch))
        {
            return new ControlPlaneOperationResult<PortalDelegationSettingsResource>(OperationResult.Conflict, null,
                "If-Match does not match ETag value", "ConcurrentOperationFailed");
        }

        existing.Resource!.UpdateFromRequest(request);

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, DelegationSettingsId, apimName,
            PortalSettingsSubresourceId, request);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, DelegationSettingsId, apimName,
            PortalSettingsETagSubresourceId, existing.Resource.ETag);

        return new ControlPlaneOperationResult<PortalDelegationSettingsResource>(OperationResult.Updated, existing.Resource);
    }
}