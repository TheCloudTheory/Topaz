namespace Topaz.Service.KeyVault.Models.Requests;

public record class VerifyKeyRequest
{
    public string? Alg { get; init; }

    /// <summary>Base64url-encoded digest that was signed.</summary>
    public string? Value { get; init; }

    /// <summary>Base64url-encoded signature to verify.</summary>
    public string? Signature { get; init; }
}
