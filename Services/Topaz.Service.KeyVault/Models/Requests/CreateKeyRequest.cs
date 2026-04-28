using System.Text.Json.Serialization;

namespace Topaz.Service.KeyVault.Models.Requests;

public record class CreateKeyRequest
{
    [JsonPropertyName("kty")]
    public string? KeyType { get; init; }

    [JsonPropertyName("key_size")]
    public int? KeySize { get; init; }

    [JsonPropertyName("crv")]
    public string? Curve { get; init; }

    [JsonPropertyName("key_ops")]
    public string[]? KeyOperations { get; init; }

    [JsonPropertyName("attributes")]
    public CreateKeyAttributes? Attributes { get; init; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; init; }

    public KeyReleasePolicy? ReleasePolicy { get; init; }
}

public record class CreateKeyAttributes
{
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }

    [JsonPropertyName("exp")]
    public long? Expires { get; init; }

    [JsonPropertyName("nbf")]
    public long? NotBefore { get; init; }

    public bool? Exportable { get; init; }
}
