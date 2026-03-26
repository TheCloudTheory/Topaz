namespace Topaz.Service.Entra.Models.Requests;

internal sealed class CreateApplicationRequest
{
    // Core
    public Application.AddInData[]? AddIns { get; init; }
    public Application.ApiApplicationData? Api { get; init; }
    public string? AppId { get; init; }
    public string? ApplicationTemplateId { get; init; }
    public Application.AppRoleData[]? AppRoles { get; init; }
    public DateTimeOffset? CreatedDateTime { get; init; }
    public DirectoryObject? CreatedOnBehalfOf { get; init; }
    public string? DefaultRedirectUri { get; init; }
    public string? Description { get; init; }
    public string? DisabledByMicrosoftStatus { get; init; }
    public string? DisplayName { get; init; }
    public string? GroupMembershipClaims { get; init; }
    public string[]? IdentifierUris { get; init; }

    // Info / UX
    public Application.InformationalUrlData? Info { get; init; }
    public bool? IsDeviceOnlyAuthSupported { get; init; }
    public bool? IsFallbackPublicClient { get; init; }
    public byte[]? Logo { get; init; }
    public string? Notes { get; init; }
    public bool? Oauth2RequirePostResponse { get; init; }

    // Claims / auth configuration
    public Application.OptionalClaimsData? OptionalClaims { get; init; }
    public Application.ParentalControlSettingsData? ParentalControlSettings { get; init; }
    public Application.PublicClientApplicationData? PublicClient { get; init; }
    public string? PublisherDomain { get; init; }
    public Application.VerifiedPublisherData? VerifiedPublisher { get; init; }
    public Application.RequiredResourceAccessData[]? RequiredResourceAccess { get; init; }
    public string? SamlMetadataUrl { get; init; }
    public string? ServiceManagementReference { get; init; }
    public string? SignInAudience { get; init; }
    public Application.SpaApplicationData? Spa { get; init; }
    public string[]? Tags { get; init; }
    public string? TokenEncryptionKeyId { get; init; }
    public string? UniqueName { get; init; }
    public Application.WebApplicationData? Web { get; init; }

    // Credentials
    public Application.KeyCredentialData[]? KeyCredentials { get; init; }
    public Application.PasswordCredentialData[]? PasswordCredentials { get; init; }

    // Misc (as documented on the Graph resource)
    public Application.CertificationData? Certification { get; init; }
    public Application.RequestSignatureVerificationData? RequestSignatureVerification { get; init; }
}