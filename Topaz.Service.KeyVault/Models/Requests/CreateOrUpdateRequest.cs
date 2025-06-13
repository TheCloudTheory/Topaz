using JetBrains.Annotations;

namespace Topaz.Service.KeyVault.Models.Requests;

// TODO: There should be a way to validate requests
internal record CreateOrUpdateKeyVaultRequest
{
    public string? Location { get; init; }
    public KeyVaultProperties? Properties { get; init; }

    internal class KeyVaultProperties
    {
        public Guid TenantId { get; set; }
        public KeyVaultSku? Sku { get; set; }

        [UsedImplicitly]
        internal class KeyVaultSku
        {
            public string? Family { get; set; }
            public string? Name { get; set; }
        }
    }
}
