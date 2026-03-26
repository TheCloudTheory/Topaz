using System.Text.Json;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models;

internal sealed class ServicePrincipal : DirectoryObject
{
    // Required property
    public required string AppId { get; init; }

    // Core properties
    public bool? AccountEnabled { get; set; }
    public string? DisplayName { get; set; }
    public string? ServicePrincipalType { get; init; }

    // Identity and naming
    public string[]? AlternativeNames { get; set; }
    public string? AppDisplayName { get; init; }
    public string? AppDescription { get; init; }
    public bool? AppRoleAssignmentRequired { get; set; }
    public string[]? ServicePrincipalNames { get; set; }

    // Publisher information
    public string? PublisherName { get; init; }
    public string? Notes { get; set; }
    public string? Description { get; set; }

    // URLs and endpoints
    public string? Homepage { get; set; }
    public string? LoginUrl { get; set; }
    public string? LogoutUrl { get; set; }
    public string[]? ReplyUrls { get; set; }
    public SamlSingleSignOnSettingsData? SamlSingleSignOnSettings { get; set; }

    // Notifications and URLs
    public string[]? NotificationEmailAddresses { get; set; }

    // Sign-in audience
    public string? SignInAudience { get; set; }

    // Tags
    public string[]? Tags { get; set; }

    // Token configuration
    public string? TokenEncryptionKeyId { get; set; }
    public string? PreferredTokenSigningKeyThumbprint { get; init; }

    // Credentials
    public KeyCredentialData[]? KeyCredentials { get; set; }
    public PasswordCredentialData[]? PasswordCredentials { get; set; }

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

    public void UpdateFrom(UpdateServicePrincipalRequest request)
    {
        if (request.AccountEnabled is not null)
        {
            AccountEnabled = request.AccountEnabled;
        }

        if (request.AlternativeNames is not null)
        {
            AlternativeNames = request.AlternativeNames;
        }

        if (request.AppRoleAssignmentRequired is not null)
        {
            AppRoleAssignmentRequired = request.AppRoleAssignmentRequired;
        }

        if (request.Description is not null)
        {
            Description = request.Description;
        }

        if (request.DisplayName is not null)
        {
            DisplayName = request.DisplayName;
        }

        if (request.Homepage is not null)
        {
            Homepage = request.Homepage;
        }

        if (request.LoginUrl is not null)
        {
            LoginUrl = request.LoginUrl;
        }

        if (request.LogoutUrl is not null)
        {
            LogoutUrl = request.LogoutUrl;
        }

        if (request.NotificationEmailAddresses is not null)
        {
            NotificationEmailAddresses = request.NotificationEmailAddresses;
        }

        if (request.ReplyUrls is not null)
        {
            ReplyUrls = request.ReplyUrls;
        }

        if (request.SamlSingleSignOnSettings is not null)
        {
            SamlSingleSignOnSettings = new SamlSingleSignOnSettingsData
            {
                RelayState = request.SamlSingleSignOnSettings.RelayState
            };
        }

        if (request.ServicePrincipalNames is not null)
        {
            ServicePrincipalNames = request.ServicePrincipalNames;
        }

        if (request.Tags is not null)
        {
            Tags = request.Tags;
        }

        if (request.TokenEncryptionKeyId is not null)
        {
            TokenEncryptionKeyId = request.TokenEncryptionKeyId;
        }

        if (request.KeyCredentials is not null)
        {
            KeyCredentials = request.KeyCredentials
                .Select(kc => new KeyCredentialData
                {
                    CustomKeyIdentifier = kc.CustomKeyIdentifier,
                    DisplayName = kc.DisplayName,
                    EndDateTime = kc.EndDateTime,
                    Key = kc.Key,
                    KeyId = kc.KeyId?.ToString(),
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
                    KeyId = pc.KeyId?.ToString(),
                    SecretText = pc.SecretText,
                    StartDateTime = pc.StartDateTime
                })
                .ToArray();
        }
    }
}