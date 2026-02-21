using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.CloudEnvironment.Models.Responses;

internal sealed class ServicePrincipalsListResponse
{
    [JsonPropertyName("@odata.context")]
    public string? OdataContext => "https://graph.microsoft.com/v1.0/$metadata#servicePrincipals";

    [JsonPropertyName("@odata.nextLink")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OdataNextLink { get; init; }

    [JsonPropertyName("@odata.count")] public int? OdataCount => Value.Length;

    public ServicePrincipal[] Value { get; init; } = [];

    public record ServicePrincipal
    {
        public string? Id { get; init; }
        public string? AppId { get; init; }
        public string? DisplayName { get; init; }
        public string? AppDisplayName { get; init; }
        public string? ServicePrincipalType { get; init; }
        public bool? AccountEnabled { get; init; }
        public string[]? ServicePrincipalNames { get; init; }
        public string? PublisherName { get; init; }
        public string? AppOwnerOrganizationId { get; init; }
        public string[]? Tags { get; init; }
        public DateTimeOffset? CreatedDateTime { get; init; }
        public PasswordCredential[]? PasswordCredentials { get; init; }
        public KeyCredential[]? KeyCredentials { get; init; }
    }

    public record PasswordCredential
    {
        [JsonPropertyName("keyId")]
        public string? KeyId { get; init; }

        [JsonPropertyName("startDateTime")]
        public DateTimeOffset? StartDateTime { get; init; }

        [JsonPropertyName("endDateTime")]
        public DateTimeOffset? EndDateTime { get; init; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; init; }
    }

    public record KeyCredential
    {
        [JsonPropertyName("keyId")]
        public string? KeyId { get; init; }

        [JsonPropertyName("startDateTime")]
        public DateTimeOffset? StartDateTime { get; init; }

        [JsonPropertyName("endDateTime")]
        public DateTimeOffset? EndDateTime { get; init; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; init; }
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
