using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

public class KeyReleaseResponse
{
    /// <summary>A compact JWS containing the key material.</summary>
    public string Value { get; init; } = string.Empty;

    public static KeyReleaseResponse New(string jws) => new() { Value = jws };

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
