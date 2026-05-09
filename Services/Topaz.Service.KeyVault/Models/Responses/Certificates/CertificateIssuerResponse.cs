using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses.Certificates;

public class CertificateIssuerResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("provider")]
    public string? Provider { get; init; }

    [JsonPropertyName("credentials")]
    public IssuerCredentials? Credentials { get; init; }

    [JsonPropertyName("org_details")]
    public OrganizationDetails? OrgDetails { get; init; }

    [JsonPropertyName("attributes")]
    public IssuerAttributes? Attributes { get; init; }

    public class IssuerCredentials
    {
        [JsonPropertyName("account_id")]
        public string? AccountId { get; init; }

        // pwd is intentionally omitted from responses (Azure never echoes passwords)
    }

    public class OrganizationDetails
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("admin_details")]
        public AdminDetails[]? AdminDetailsList { get; init; }
    }

    public class AdminDetails
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

    public class IssuerAttributes
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; init; }

        [JsonPropertyName("created")]
        public long Created { get; init; }

        [JsonPropertyName("updated")]
        public long Updated { get; init; }
    }

    public static CertificateIssuerResponse ForVault(
        string vaultName,
        string issuerName,
        string provider,
        IssuerCredentials? credentials,
        OrganizationDetails? orgDetails,
        IssuerAttributes attributes) => new()
    {
        Id = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/certificates/issuers/{issuerName}",
        Provider = provider,
        Credentials = credentials,
        OrgDetails = orgDetails,
        Attributes = attributes
    };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
