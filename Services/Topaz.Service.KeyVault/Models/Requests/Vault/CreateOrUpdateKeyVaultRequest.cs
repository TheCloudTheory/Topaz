using Azure.ResourceManager.Resources;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Requests.Vault;

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
        public KeyVaultResourceProperties.AccessPolicyEntry[]? AccessPolicies { get; set; }

        [UsedImplicitly]
        internal class KeyVaultSku
        {
            public string? Family { get; set; }
            public string? Name { get; set; }
        }
    }

    public static CreateOrUpdateKeyVaultRequest From(GenericResourceData data)
    {
        return new CreateOrUpdateKeyVaultRequest
        {
            Location = data.Location,
            Tags = data.Tags,
            Properties = data.Properties.ToObjectFromJson<KeyVaultProperties>(GlobalSettings.JsonOptions)
        };
    }
}
