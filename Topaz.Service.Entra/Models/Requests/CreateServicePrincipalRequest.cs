namespace Topaz.Service.Entra.Models.Requests;

internal sealed class CreateServicePrincipalRequest
{
    // Required property
    public required string AppId { get; init; }

    // Core properties
    public bool? AccountEnabled { get; init; }
    public string? DisplayName { get; init; }
    public string? ServicePrincipalType { get; init; }

    // Identity and naming
    public string[]? AlternativeNames { get; init; }
    public string? AppDisplayName { get; init; }
    public string? AppDescription { get; init; }
    public bool? AppRoleAssignmentRequired { get; init; }
    public string[]? ServicePrincipalNames { get; init; }

    // Publisher information
    public string? PublisherName { get; init; }
    public string? Notes { get; init; }
    public string? Description { get; init; }

    // URLs and endpoints
    public string? Homepage { get; init; }
    public string? LoginUrl { get; init; }
    public string? LogoutUrl { get; init; }
    public string[]? ReplyUrls { get; init; }
    public SamlSingleSignOnSettingsData? SamlSingleSignOnSettings { get; init; }

    // Notifications and URLs
    public string[]? NotificationEmailAddresses { get; init; }

    // Sign-in audience
    public string? SignInAudience { get; init; }

    // Tags
    public string[]? Tags { get; init; }

    // Token configuration
    public string? TokenEncryptionKeyId { get; init; }
    public string? PreferredTokenSigningKeyThumbprint { get; init; }

    // Credentials
    public KeyCredentialData[]? KeyCredentials { get; init; }
    public PasswordCredentialData[]? PasswordCredentials { get; init; }

    internal sealed class SamlSingleSignOnSettingsData
    {
        public string? RelayState { get; init; }
    }

    internal sealed class KeyCredentialData
    {
        public string? CustomKeyIdentifier { get; init; }
        public string? DisplayName { get; init; }
        public DateTimeOffset? EndDateTime { get; init; }
        public byte[]? Key { get; init; }
        public string? KeyId { get; init; }
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
        public string? KeyId { get; init; }
        public string? SecretText { get; init; }
        public DateTimeOffset? StartDateTime { get; init; }
    }
}   