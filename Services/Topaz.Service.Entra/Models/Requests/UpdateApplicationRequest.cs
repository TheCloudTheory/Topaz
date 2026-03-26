using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Topaz.Service.Entra.Models.Requests;

internal sealed class UpdateApplicationRequest
{
    // Commonly updated core properties
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public string? SignInAudience { get; init; }
    public string[]? IdentifierUris { get; init; }
    public string? GroupMembershipClaims { get; init; }
    public string[]? Tags { get; init; }
    public string? Notes { get; init; }

    // Complex/nested shapes
    public AddInData[]? AddIns { get; init; }
    public ApiApplicationData? Api { get; init; }
    public AppRoleData[]? AppRoles { get; init; }
    public InformationalUrlData? Info { get; init; }
    public bool? IsFallbackPublicClient { get; init; }
    public OptionalClaimsData? OptionalClaims { get; init; }
    public ParentalControlSettingsData? ParentalControlSettings { get; init; }
    public PublicClientApplicationData? PublicClient { get; init; }
    public RequiredResourceAccessData[]? RequiredResourceAccess { get; init; }
    public SpaApplicationData? Spa { get; init; }
    public WebApplicationData? Web { get; init; }

    // Credentials / keys
    public KeyCredentialData[]? KeyCredentials { get; init; }
    public PasswordCredentialData[]? PasswordCredentials { get; init; }
    public string? TokenEncryptionKeyId { get; init; }

    /// <summary>
    /// Supports any additional/unknown fields without breaking deserialization.
    /// Helpful because Graph periodically adds fields and emulators may accept extras.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

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
        public string[]? AllowedMemberTypes { get; init; }
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

        /// <summary>
        /// Keeping as string because Graph uses a complex type in some SDKs and
        /// this emulator may not need strict modeling yet.
        /// </summary>
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

        // If provided, emulator can accept it; if null it won't serialize.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SecretText { get; init; }

        public DateTimeOffset? StartDateTime { get; init; }
    }
}