using System.Text.Json.Serialization;

namespace Topaz.Service.KeyVault.Models.Requests;

public record class ImportKeyRequest
{
    [JsonPropertyName("key")]
    public ImportKeyJwk? Key { get; init; }

    [JsonPropertyName("hsm")]
    public bool? Hsm { get; init; }

    [JsonPropertyName("attributes")]
    public CreateKeyAttributes? Attributes { get; init; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; init; }
}

/// <summary>
/// JSON Web Key supplied by the caller in an Import Key request.
/// Only the public (and optionally private) components used by Topaz are modelled here.
/// </summary>
public record class ImportKeyJwk
{
    [JsonPropertyName("kty")]
    public string? KeyType { get; init; }

    [JsonPropertyName("key_ops")]
    public string[]? KeyOperations { get; init; }

    // RSA
    [JsonPropertyName("n")]
    public string? N { get; init; }

    [JsonPropertyName("e")]
    public string? E { get; init; }

    // RSA private components
    [JsonPropertyName("d")]
    public string? D { get; init; }

    [JsonPropertyName("p")]
    public string? P { get; init; }

    [JsonPropertyName("q")]
    public string? Q { get; init; }

    [JsonPropertyName("dp")]
    public string? DP { get; init; }

    [JsonPropertyName("dq")]
    public string? DQ { get; init; }

    [JsonPropertyName("qi")]
    public string? InverseQ { get; init; }

    // EC
    [JsonPropertyName("crv")]
    public string? Crv { get; init; }

    [JsonPropertyName("x")]
    public string? X { get; init; }

    [JsonPropertyName("y")]
    public string? Y { get; init; }
}
