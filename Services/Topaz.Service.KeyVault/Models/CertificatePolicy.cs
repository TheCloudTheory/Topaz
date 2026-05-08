using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models;

public record CertificatePolicy
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("issuer")]
    public IssuerParameters? Issuer { get; set; }

    [JsonPropertyName("key_props")]
    public KeyProperties? KeyProps { get; set; }

    [JsonPropertyName("secret_props")]
    public SecretProperties? SecretProps { get; set; }

    [JsonPropertyName("x509_props")]
    public X509CertificateProperties? X509Props { get; set; }

    [JsonPropertyName("lifetime_actions")]
    public LifetimeAction[]? LifetimeActions { get; set; }

    [JsonPropertyName("attributes")]
    public PolicyAttributes? Attributes { get; set; }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    public record IssuerParameters
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "Self";

        [JsonPropertyName("cty")]
        public string? CertificateType { get; set; }
    }

    public record KeyProperties
    {
        [JsonPropertyName("exportable")]
        public bool Exportable { get; set; } = true;

        [JsonPropertyName("kty")]
        public string KeyType { get; set; } = "RSA";

        [JsonPropertyName("key_size")]
        public int KeySize { get; set; } = 2048;

        [JsonPropertyName("reuse_key")]
        public bool ReuseKey { get; set; }
    }

    public record SecretProperties
    {
        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = "application/x-pkcs12";
    }

    public record X509CertificateProperties
    {
        [JsonPropertyName("subject")]
        public string Subject { get; set; } = "CN=DefaultPolicy";

        [JsonPropertyName("validity_months")]
        public int ValidityMonths { get; set; } = 12;

        [JsonPropertyName("key_usage")]
        public string[]? KeyUsage { get; set; }

        [JsonPropertyName("sans")]
        public SubjectAlternativeNames? Sans { get; set; }
    }

    public record SubjectAlternativeNames
    {
        [JsonPropertyName("dns_names")]
        public string[]? DnsNames { get; set; }

        [JsonPropertyName("emails")]
        public string[]? Emails { get; set; }

        [JsonPropertyName("upns")]
        public string[]? Upns { get; set; }
    }

    public record LifetimeAction
    {
        [JsonPropertyName("trigger")]
        public LifetimeTrigger? Trigger { get; set; }

        [JsonPropertyName("action")]
        public LifetimeActionType? Action { get; set; }
    }

    public record LifetimeTrigger
    {
        [JsonPropertyName("lifetime_percentage")]
        public int? LifetimePercentage { get; set; }

        [JsonPropertyName("days_before_expiry")]
        public int? DaysBeforeExpiry { get; set; }
    }

    public record LifetimeActionType
    {
        [JsonPropertyName("action_type")]
        public string ActionType { get; set; } = "AutoRenew";
    }

    public record PolicyAttributes
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("updated")]
        public long Updated { get; set; }
    }
}
