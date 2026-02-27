using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Topaz.Service.Entra.Models.Requests;

internal sealed class UpdateServicePrincipalRequest
{
    public bool? AccountEnabled { get; init; }
    public string[]? AlternativeNames { get; init; }
    public bool? AppRoleAssignmentRequired { get; init; }
    public string? Description { get; init; }
    public string? DisplayName { get; init; }
    public string? Homepage { get; init; }
    public string? LoginUrl { get; init; }
    public string? LogoutUrl { get; init; }
    public string[]? NotificationEmailAddresses { get; init; }
    public string? PreferredSingleSignOnMode { get; init; }
    public string[]? ReplyUrls { get; init; }
    public SamlSingleSignOnSettingsData? SamlSingleSignOnSettings { get; init; }
    public string[]? ServicePrincipalNames { get; init; }
    public string[]? Tags { get; init; }
    public string? TokenEncryptionKeyId { get; init; }

    public KeyCredentialData[]? KeyCredentials { get; init; }
    public PasswordCredentialData[]? PasswordCredentials { get; init; }

    /// <summary>
    /// Represents custom security attributes payload.
    /// Modeled as a free-form JSON object to match Graph's shape.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonObject? CustomSecurityAttributes { get; init; }

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