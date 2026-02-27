using System.Text.Json;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models;

internal class Application : DirectoryObject
{
    // Core
    public AddInData[]? AddIns { get; set; }
    public ApiApplicationData? Api { get; set; }
    public string? AppId { get; init; }
    public string? ApplicationTemplateId { get; init; }
    public AppRoleData[]? AppRoles { get; set; }
    public DateTimeOffset? CreatedDateTime { get; init; }
    public DirectoryObject? CreatedOnBehalfOf { get; init; }
    public string? DefaultRedirectUri { get; init; }
    public string? Description { get; set; }
    public string? DisabledByMicrosoftStatus { get; init; }
    public string? DisplayName { get; set; }
    public string? GroupMembershipClaims { get; set; }
    public string[]? IdentifierUris { get; set; }

    // Info / UX
    public InformationalUrlData? Info { get; set; }
    public bool? IsDeviceOnlyAuthSupported { get; init; }
    public bool? IsFallbackPublicClient { get; set; }
    public byte[]? Logo { get; init; }
    public string? Notes { get; set; }
    public bool? Oauth2RequirePostResponse { get; init; }

    // Claims / auth configuration
    public OptionalClaimsData? OptionalClaims { get; set; }
    public ParentalControlSettingsData? ParentalControlSettings { get; set; }
    public PublicClientApplicationData? PublicClient { get; set; }
    public string? PublisherDomain { get; init; }
    public VerifiedPublisherData? VerifiedPublisher { get; init; }
    public RequiredResourceAccessData[]? RequiredResourceAccess { get; set; }
    public string? SamlMetadataUrl { get; init; }
    public string? ServiceManagementReference { get; init; }
    public string? SignInAudience { get; set; }
    public SpaApplicationData? Spa { get; set; }
    public string[]? Tags { get; set; }
    public string? TokenEncryptionKeyId { get; set; }
    public string? UniqueName { get; init; }
    public WebApplicationData? Web { get; set; }

    // Credentials
    public KeyCredentialData[]? KeyCredentials { get; set; }
    public PasswordCredentialData[]? PasswordCredentials { get; set; }

    // Misc (as documented on the Graph resource)
    public CertificationData? Certification { get; init; }
    public RequestSignatureVerificationData? RequestSignatureVerification { get; init; }

    // ----------------------------
    // Nested types (Graph shapes)
    // ----------------------------

    internal sealed class AddInData
    {
        public string? Id { get; init; }
        public string? Type { get; init; }
        public object? Properties { get; init; }
    }

    internal sealed class ApiApplicationData
    {
        public PreAuthorizedApplicationData[]? PreAuthorizedApplications { get; init; }
        public PermissionScopeData[]? Oauth2PermissionScopes { get; init; }
        public string? KnownClientApplications { get; init; }
        public bool? RequestedAccessTokenVersion { get; init; }
        public string? AcceptMappedClaims { get; init; }
    }

    internal sealed class AppRoleData
    {
        public Guid? Id { get; init; }
        public string? AllowedMemberTypes { get; init; }
        public string? Description { get; init; }
        public string? DisplayName { get; init; }
        public bool? IsEnabled { get; init; }
        public string? Origin { get; init; }
        public string? Value { get; init; }
    }

    internal sealed class InformationalUrlData
    {
        public string? LogoUrl { get; init; }
        public string? MarketingUrl { get; init; }
        public string? PrivacyStatementUrl { get; init; }
        public string? SupportUrl { get; init; }
        public string? TermsOfServiceUrl { get; init; }
    }

    internal sealed class OptionalClaimsData
    {
        public OptionalClaimData[]? AccessToken { get; init; }
        public OptionalClaimData[]? IdToken { get; init; }
        public OptionalClaimData[]? Saml2Token { get; init; }
    }

    internal sealed class OptionalClaimData
    {
        public string? Name { get; init; }
        public string? Source { get; init; }
        public bool? Essential { get; init; }
        public string[]? AdditionalProperties { get; init; }
    }

    internal sealed class ParentalControlSettingsData
    {
        public string? CountriesBlockedForMinors { get; init; }
        public string? LegalAgeGroupRule { get; init; }
    }

    internal sealed class PublicClientApplicationData
    {
        public string[]? RedirectUris { get; init; }
    }

    internal sealed class SpaApplicationData
    {
        public string[]? RedirectUris { get; init; }
    }

    internal sealed class WebApplicationData
    {
        public string? HomePageUrl { get; init; }
        public string? LogoutUrl { get; init; }
        public string[]? RedirectUris { get; init; }
        public ImplicitGrantSettingsData? ImplicitGrantSettings { get; init; }
        public string? RedirectUriSettings { get; init; }
    }

    internal sealed class ImplicitGrantSettingsData
    {
        public bool? EnableAccessTokenIssuance { get; init; }
        public bool? EnableIdTokenIssuance { get; init; }
    }

    internal sealed class RequiredResourceAccessData
    {
        public Guid? ResourceAppId { get; init; }
        public ResourceAccessData[]? ResourceAccess { get; init; }
    }

    internal sealed class ResourceAccessData
    {
        public Guid? Id { get; init; }
        public string? Type { get; init; }
    }

    internal sealed class PreAuthorizedApplicationData
    {
        public Guid? AppId { get; init; }
        public string[]? DelegatedPermissionIds { get; init; }
    }

    internal sealed class PermissionScopeData
    {
        public Guid? Id { get; init; }
        public string? AdminConsentDescription { get; init; }
        public string? AdminConsentDisplayName { get; init; }
        public bool? IsEnabled { get; init; }
        public string? Type { get; init; }
        public string? UserConsentDescription { get; init; }
        public string? UserConsentDisplayName { get; init; }
        public string? Value { get; init; }
    }

    internal sealed class VerifiedPublisherData
    {
        public string? DisplayName { get; init; }
        public string? VerifiedPublisherId { get; init; }
        public string? AddedDateTime { get; init; }
    }

    internal sealed class KeyCredentialData
    {
        public string? CustomKeyIdentifier { get; init; }
        public string? DisplayName { get; init; }
        public DateTimeOffset? EndDateTime { get; init; }
        public byte[]? Key { get; init; }
        public Guid? KeyId { get; init; }
        public DateTimeOffset? StartDateTime { get; init; }
        public string? Type { get; init; }
        public string? Usage { get; init; }
    }

    internal sealed class PasswordCredentialData
    {
        public string? CustomKeyIdentifier { get; init; }
        public string? DisplayName { get; init; }
        public DateTimeOffset? EndDateTime { get; init; }
        public string? Hint { get; init; }
        public Guid? KeyId { get; init; }
        public string? SecretText { get; init; }
        public DateTimeOffset? StartDateTime { get; init; }
    }

    internal sealed class CertificationData
    {
        public string? CertificationDetailsUrl { get; init; }
        public string? CertificationExpirationDateTime { get; init; }
        public string? CertificationStartDateTime { get; init; }
        public string? IsPublisherAttested { get; init; }
    }

    internal sealed class RequestSignatureVerificationData
    {
        public string? AllowedWeakAlgorithms { get; init; }
        public bool? IsSignedRequestRequired { get; init; }
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }

    public static Application FromRequest(ApplicationIdentifier applicationIdentifier, CreateApplicationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new Application
        {
            // Entity / DirectoryObject
            Id = Guid.NewGuid().ToString(),
            DeletedDateTime = null,

            // Core
            AddIns = request.AddIns,
            Api = request.Api,
            AppId = applicationIdentifier.Value,
            ApplicationTemplateId = request.ApplicationTemplateId,
            AppRoles = request.AppRoles,
            CreatedDateTime = DateTimeOffset.UtcNow,
            CreatedOnBehalfOf = request.CreatedOnBehalfOf,
            DefaultRedirectUri = request.DefaultRedirectUri,
            Description = request.Description,
            DisabledByMicrosoftStatus = request.DisabledByMicrosoftStatus,
            DisplayName = request.DisplayName,
            GroupMembershipClaims = request.GroupMembershipClaims,
            IdentifierUris = request.IdentifierUris,

            // Info / UX
            Info = request.Info,
            IsDeviceOnlyAuthSupported = request.IsDeviceOnlyAuthSupported,
            IsFallbackPublicClient = request.IsFallbackPublicClient,
            Logo = request.Logo,
            Notes = request.Notes,
            Oauth2RequirePostResponse = request.Oauth2RequirePostResponse,

            // Claims / auth configuration
            OptionalClaims = request.OptionalClaims,
            ParentalControlSettings = request.ParentalControlSettings,
            PublicClient = request.PublicClient,
            PublisherDomain = request.PublisherDomain,
            VerifiedPublisher = request.VerifiedPublisher,
            RequiredResourceAccess = request.RequiredResourceAccess,
            SamlMetadataUrl = request.SamlMetadataUrl,
            ServiceManagementReference = request.ServiceManagementReference,
            SignInAudience = request.SignInAudience,
            Spa = request.Spa,
            Tags = request.Tags,
            TokenEncryptionKeyId = request.TokenEncryptionKeyId,
            UniqueName = request.UniqueName,
            Web = request.Web,

            // Credentials
            KeyCredentials = request.KeyCredentials,
            PasswordCredentials = request.PasswordCredentials,

            // Misc
            Certification = request.Certification,
            RequestSignatureVerification = request.RequestSignatureVerification
        };
    }

    public void UpdateFrom(UpdateApplicationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Simple scalars / collections
        if (request.DisplayName is not null) DisplayName = request.DisplayName;
        if (request.Description is not null) Description = request.Description;
        if (request.SignInAudience is not null) SignInAudience = request.SignInAudience;
        if (request.IdentifierUris is not null) IdentifierUris = request.IdentifierUris;
        if (request.GroupMembershipClaims is not null) GroupMembershipClaims = request.GroupMembershipClaims;
        if (request.Tags is not null) Tags = request.Tags;
        if (request.Notes is not null) Notes = request.Notes;

        // Complex shapes (replace-as-a-whole, which matches typical PATCH semantics for nested objects)
        if (request.AddIns is not null)
        {
            AddIns = request.AddIns
                .Select(a => new AddInData
                {
                    Id = a.Id,
                    Type = a.Type,
                    Properties = a.Properties
                })
                .ToArray();
        }

        if (request.Api is not null)
        {
            Api = new ApiApplicationData
            {
                KnownClientApplications = request.Api.KnownClientApplications,
                RequestedAccessTokenVersion = request.Api.RequestedAccessTokenVersion,
                AcceptMappedClaims = request.Api.AcceptMappedClaims,
                PreAuthorizedApplications = request.Api.PreAuthorizedApplications?
                    .Select(p => new PreAuthorizedApplicationData
                    {
                        AppId = p.AppId,
                        DelegatedPermissionIds = p.DelegatedPermissionIds
                    })
                    .ToArray(),
                Oauth2PermissionScopes = request.Api.Oauth2PermissionScopes?
                    .Select(s => new PermissionScopeData
                    {
                        Id = s.Id,
                        AdminConsentDescription = s.AdminConsentDescription,
                        AdminConsentDisplayName = s.AdminConsentDisplayName,
                        IsEnabled = s.IsEnabled,
                        Type = s.Type,
                        UserConsentDescription = s.UserConsentDescription,
                        UserConsentDisplayName = s.UserConsentDisplayName,
                        Value = s.Value
                    })
                    .ToArray()
            };
        }

        if (request.AppRoles is not null)
        {
            AppRoles = request.AppRoles
                .Select(r => new AppRoleData
                {
                    Id = r.Id,
                    AllowedMemberTypes = r.AllowedMemberTypes is null ? null : string.Join(",", r.AllowedMemberTypes),
                    Description = r.Description,
                    DisplayName = r.DisplayName,
                    IsEnabled = r.IsEnabled,
                    Origin = r.Origin,
                    Value = r.Value
                })
                .ToArray();
        }

        if (request.Info is not null)
        {
            Info = new InformationalUrlData
            {
                LogoUrl = request.Info.LogoUrl,
                MarketingUrl = request.Info.MarketingUrl,
                PrivacyStatementUrl = request.Info.PrivacyStatementUrl,
                SupportUrl = request.Info.SupportUrl,
                TermsOfServiceUrl = request.Info.TermsOfServiceUrl
            };
        }

        if (request.IsFallbackPublicClient is not null)
        {
            IsFallbackPublicClient = request.IsFallbackPublicClient;
        }

        if (request.OptionalClaims is not null)
        {
            OptionalClaims = new OptionalClaimsData
            {
                AccessToken = request.OptionalClaims.AccessToken?
                    .Select(c => new OptionalClaimData
                    {
                        Name = c.Name,
                        Source = c.Source,
                        Essential = c.Essential,
                        AdditionalProperties = c.AdditionalProperties
                    })
                    .ToArray(),
                IdToken = request.OptionalClaims.IdToken?
                    .Select(c => new OptionalClaimData
                    {
                        Name = c.Name,
                        Source = c.Source,
                        Essential = c.Essential,
                        AdditionalProperties = c.AdditionalProperties
                    })
                    .ToArray(),
                Saml2Token = request.OptionalClaims.Saml2Token?
                    .Select(c => new OptionalClaimData
                    {
                        Name = c.Name,
                        Source = c.Source,
                        Essential = c.Essential,
                        AdditionalProperties = c.AdditionalProperties
                    })
                    .ToArray()
            };
        }

        if (request.ParentalControlSettings is not null)
        {
            ParentalControlSettings = new ParentalControlSettingsData
            {
                CountriesBlockedForMinors = request.ParentalControlSettings.CountriesBlockedForMinors,
                LegalAgeGroupRule = request.ParentalControlSettings.LegalAgeGroupRule
            };
        }

        if (request.PublicClient is not null)
        {
            PublicClient = new PublicClientApplicationData
            {
                RedirectUris = request.PublicClient.RedirectUris
            };
        }

        if (request.RequiredResourceAccess is not null)
        {
            RequiredResourceAccess = request.RequiredResourceAccess
                .Select(rra => new RequiredResourceAccessData
                {
                    ResourceAppId = rra.ResourceAppId,
                    ResourceAccess = rra.ResourceAccess?
                        .Select(ra => new ResourceAccessData
                        {
                            Id = ra.Id,
                            Type = ra.Type
                        })
                        .ToArray()
                })
                .ToArray();
        }

        if (request.Spa is not null)
        {
            Spa = new SpaApplicationData
            {
                RedirectUris = request.Spa.RedirectUris
            };
        }

        if (request.Web is not null)
        {
            Web = new WebApplicationData
            {
                HomePageUrl = request.Web.HomePageUrl,
                LogoutUrl = request.Web.LogoutUrl,
                RedirectUris = request.Web.RedirectUris,
                RedirectUriSettings = request.Web.RedirectUriSettings,
                ImplicitGrantSettings = request.Web.ImplicitGrantSettings is null
                    ? null
                    : new ImplicitGrantSettingsData
                    {
                        EnableAccessTokenIssuance = request.Web.ImplicitGrantSettings.EnableAccessTokenIssuance,
                        EnableIdTokenIssuance = request.Web.ImplicitGrantSettings.EnableIdTokenIssuance
                    }
            };
        }

        // Credentials / keys
        if (request.KeyCredentials is not null)
        {
            KeyCredentials = request.KeyCredentials
                .Select(kc => new KeyCredentialData
                {
                    CustomKeyIdentifier = kc.CustomKeyIdentifier,
                    DisplayName = kc.DisplayName,
                    EndDateTime = kc.EndDateTime,
                    Key = kc.Key,
                    KeyId = kc.KeyId,
                    StartDateTime = kc.StartDateTime,
                    Type = kc.Type,
                    Usage = kc.Usage
                })
                .ToArray();
        }

        if (request.PasswordCredentials is not null)
        {
            PasswordCredentials = request.PasswordCredentials
                .Select(pc => new PasswordCredentialData
                {
                    CustomKeyIdentifier = pc.CustomKeyIdentifier,
                    DisplayName = pc.DisplayName,
                    EndDateTime = pc.EndDateTime,
                    Hint = pc.Hint,
                    KeyId = pc.KeyId,
                    SecretText = pc.SecretText,
                    StartDateTime = pc.StartDateTime
                })
                .ToArray();
        }

        if (request.TokenEncryptionKeyId is not null)
        {
            TokenEncryptionKeyId = request.TokenEncryptionKeyId;
        }
    }
}