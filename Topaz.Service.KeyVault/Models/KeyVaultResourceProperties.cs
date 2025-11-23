using JetBrains.Annotations;
using Topaz.Service.KeyVault.Models.Requests;

namespace Topaz.Service.KeyVault.Models;

internal sealed class KeyVaultResourceProperties
{
    public KeyVaultResourceProperties()
    {
    }

    private KeyVaultResourceProperties(string keyVaultName)
    {
        VaultUri = $"https://{keyVaultName}.vault.azure.net";
    }
    
    public Guid TenantId { get; init; }
    public KeyVaultSku? Sku { get; set; }
    public bool EnabledForDeployment { get; set; }
    public bool EnabledForDiskEncryption { get; set; }
    public bool EnabledForTemplateDeployment { get; set; }
    public bool EnableSoftDelete { get; set; } = true;
    public bool EnablePurgeProtection { get; set; }
    public bool EnableRbacAuthorization { get; set; }
    public uint SoftDeleteRetentionInDays  { get; set; } = 90;
    public string? VaultUri { get; set; }

    [UsedImplicitly]
    internal class KeyVaultSku
    {
        public string? Family { get; set; }
        public string? Name { get; set; }
    }

    public static KeyVaultResourceProperties Default(string keyVaultName) => new(keyVaultName)
    {
        TenantId = Guid.Empty,
        Sku = new KeyVaultSku
        {
            Family = "A",
            Name = "Standard"
        }
    };

    public static KeyVaultResourceProperties FromRequest(string keyVaultName, CreateOrUpdateKeyVaultRequest request)
    {
        if (request.Properties == null)
        {
            throw new ArgumentNullException(nameof(request.Properties));
        }
        
        return new KeyVaultResourceProperties(keyVaultName)
        {
            EnabledForDeployment = request.Properties.EnabledForDeployment,
            EnabledForDiskEncryption = request.Properties.EnabledForDiskEncryption,
            EnabledForTemplateDeployment = request.Properties.EnabledForTemplateDeployment,
            EnableSoftDelete = request.Properties.EnableSoftDelete,
            EnablePurgeProtection = request.Properties.EnablePurgeProtection,
            EnableRbacAuthorization = request.Properties.EnableRbacAuthorization,
            SoftDeleteRetentionInDays = request.Properties.SoftDeleteRetentionInDays,
            TenantId = request.Properties.TenantId,
            Sku = new KeyVaultSku
            {
                Family = request.Properties.Sku?.Family,
                Name = request.Properties.Sku?.Name
            }
        };
    }
}