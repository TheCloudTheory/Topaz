using System.Text.Json.Serialization;

namespace Topaz.Service.KeyVault.Models.Requests.Certificates;

public record SetCertificateIssuerRequest
{
    [JsonPropertyName("provider")]
    public string? Provider { get; init; }

    [JsonPropertyName("credentials")]
    public IssuerCredentials? Credentials { get; init; }

    [JsonPropertyName("org_details")]
    public OrganizationDetails? OrgDetails { get; init; }

    [JsonPropertyName("attributes")]
    public IssuerAttributes? Attributes { get; init; }

    public record IssuerCredentials
    {
        [JsonPropertyName("account_id")]
        public string? AccountId { get; init; }

        [JsonPropertyName("pwd")]
        public string? Password { get; init; }
    }

    public record OrganizationDetails
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("admin_details")]
        public AdminDetails[]? AdminDetailsList { get; init; }
    }

    public record AdminDetails
    {
        [JsonPropertyName("first_name")]
        public string? FirstName { get; init; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("phone")]
        public string? Phone { get; init; }
    }

    public record IssuerAttributes
    {
        [JsonPropertyName("enabled")]
        public bool? Enabled { get; init; }
    }
}
