using JetBrains.Annotations;

namespace Topaz.Service.KeyVault.Models.Requests;

public sealed class UpdateKeyVaultRequest
{
    public KeyVaultProperties? Properties { get; init; }

    [UsedImplicitly]
    public class KeyVaultProperties
    {
        public string? CreateMode { get; set; }
    }
}