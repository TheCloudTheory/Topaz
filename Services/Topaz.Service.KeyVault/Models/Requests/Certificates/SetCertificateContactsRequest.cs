using System.Text.Json.Serialization;

namespace Topaz.Service.KeyVault.Models.Requests.Certificates;

public record SetCertificateContactsRequest
{
    [JsonPropertyName("contacts")]
    public ContactEntry[]? ContactList { get; init; }

    public record ContactEntry
    {
        [JsonPropertyName("email")]
        public string? EmailAddress { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("phone")]
        public string? Phone { get; init; }
    }
}
