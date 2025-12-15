using JetBrains.Annotations;

namespace Topaz.Service.KeyVault.Models.Requests;

// TODO: There should be a way to validate requests
internal record CreateOrUpdateKeyVaultRequest
{
    public string? Location { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
    public KeyVaultProperties? Properties { get; init; }

    internal class KeyVaultProperties
    {
        public Guid? TenantId { get; set; }
        public KeyVaultSku? Sku { get; set; }
        public bool? EnabledForDeployment { get; set; }
        public bool? EnabledForDiskEncryption { get; set; }
        public bool? EnabledForTemplateDeployment { get; set; }
        public bool? EnableSoftDelete { get; set; }
        public bool? EnablePurgeProtection { get; set; }
        public bool? EnableRbacAuthorization { get; set; }
        public uint? SoftDeleteRetentionInDays  { get; set; }
        public string? CreateMode { get; set; }

        [UsedImplicitly]
        internal class KeyVaultSku
        {
            public string? Family { get; set; }
            public string? Name { get; set; }
        }
    }
}
