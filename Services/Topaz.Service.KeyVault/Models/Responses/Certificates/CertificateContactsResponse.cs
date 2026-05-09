using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses.Certificates;

public class CertificateContactsResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("contacts")]
    public ContactEntry[]? ContactList { get; init; }

    public class ContactEntry
    {
        [JsonPropertyName("email")]
        public string? EmailAddress { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("phone")]
        public string? Phone { get; init; }
    }

    public static CertificateContactsResponse ForVault(string vaultName, ContactEntry[]? contacts) => new()
    {
        Id = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/certificates/contacts",
        ContactList = contacts ?? []
    };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
