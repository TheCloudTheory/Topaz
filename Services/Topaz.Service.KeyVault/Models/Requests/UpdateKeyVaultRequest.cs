using JetBrains.Annotations;

namespace Topaz.Service.KeyVault.Models.Requests;

public sealed class UpdateKeyVaultRequest
{
    public IDictionary<string, string>? Tags { get; init; }
    public KeyVaultProperties? Properties { get; init; }

    [UsedImplicitly]
    public class KeyVaultProperties
    {
        public string? CreateMode { get; set; }
    }
}