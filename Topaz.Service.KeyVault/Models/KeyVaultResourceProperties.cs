using JetBrains.Annotations;

namespace Topaz.Service.KeyVault.Models;

internal sealed class KeyVaultResourceProperties
{
    public Guid TenantId { get; set; }
    public KeyVaultSku? Sku { get; set; }

    [UsedImplicitly]
    internal class KeyVaultSku
    {
        public string? Family { get; set; }
        public string? Name { get; set; }
    }

    public static KeyVaultResourceProperties Default => new()
    {
        TenantId = Guid.Empty,
        Sku = new KeyVaultSku
        {
            Family = "A",
            Name = "Standard"
        }
    };
}