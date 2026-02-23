using System.Text.Json;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models;

internal sealed class ServicePrincipal : DirectoryObject
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

    public static ServicePrincipal FromRequest(CreateServicePrincipalRequest request)
    {
        return new ServicePrincipal
        {
            Id = Guid.NewGuid().ToString(),
            AppId = request.AppId,
            AccountEnabled = request.AccountEnabled,
            DisplayName = request.DisplayName,
            ServicePrincipalType = request.ServicePrincipalType,
            AlternativeNames = request.AlternativeNames,
            AppDisplayName = request.AppDisplayName,
            AppDescription = request.AppDescription,
            AppRoleAssignmentRequired = request.AppRoleAssignmentRequired,
            ServicePrincipalNames = request.ServicePrincipalNames,
            PublisherName = request.PublisherName,
            Notes = request.Notes,
            Description = request.Description,
            Homepage = request.Homepage,
            LoginUrl = request.LoginUrl,
            LogoutUrl = request.LogoutUrl,
            ReplyUrls = request.ReplyUrls,
            SamlSingleSignOnSettings = request.SamlSingleSignOnSettings != null
                ? new SamlSingleSignOnSettingsData
                {
                    RelayState = request.SamlSingleSignOnSettings.RelayState
                }
                : null,
            NotificationEmailAddresses = request.NotificationEmailAddresses,
            SignInAudience = request.SignInAudience,
            Tags = request.Tags,
            TokenEncryptionKeyId = request.TokenEncryptionKeyId,
            PreferredTokenSigningKeyThumbprint = request.PreferredTokenSigningKeyThumbprint,
            KeyCredentials = request.KeyCredentials?
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
                .ToArray(),
            PasswordCredentials = request.PasswordCredentials?
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
                .ToArray()
        };
    }
    
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}