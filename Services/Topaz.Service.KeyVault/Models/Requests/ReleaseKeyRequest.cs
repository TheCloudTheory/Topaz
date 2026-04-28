namespace Topaz.Service.KeyVault.Models.Requests;

public record class ReleaseKeyRequest
{
    /// <summary>The attestation assertion (any non-empty string is accepted by the emulator).</summary>
    public string? Target { get; init; }

    /// <summary>The encryption algorithm used to wrap the key material. Accepted but not enforced by the emulator.</summary>
    public string? Enc { get; init; }

    /// <summary>A client-provided nonce for freshness. Accepted but not enforced by the emulator.</summary>
    public string? Nonce { get; init; }
}
