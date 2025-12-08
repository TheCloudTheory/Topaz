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
        VaultUri = $"https://{keyVaultName}.keyvault.topaz.local.dev";
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
            EnabledForDeployment = request.Properties.EnabledForDeployment.GetValueOrDefault(false),
            EnabledForDiskEncryption = request.Properties.EnabledForDiskEncryption.GetValueOrDefault(false),
            EnabledForTemplateDeployment = request.Properties.EnabledForTemplateDeployment.GetValueOrDefault(false),
            EnableSoftDelete = request.Properties.EnableSoftDelete.GetValueOrDefault(true),
            EnablePurgeProtection = request.Properties.EnablePurgeProtection.GetValueOrDefault(false),
            EnableRbacAuthorization = request.Properties.EnableRbacAuthorization.GetValueOrDefault(false),
            SoftDeleteRetentionInDays = request.Properties.SoftDeleteRetentionInDays.GetValueOrDefault(90),
            Sku = new KeyVaultSku
            {
                Family = request.Properties.Sku?.Family,
                Name = request.Properties.Sku?.Name
            }
        };
    }

    public static void UpdateFromRequest(KeyVaultResource resource, CreateOrUpdateKeyVaultRequest request)
    {
        if (request.Properties == null)
        {
            throw new ArgumentNullException(nameof(request.Properties));
        }

        resource.Properties.EnabledForDeployment = request.Properties.EnabledForDeployment.GetValueOrDefault(false);
        resource.Properties.EnabledForDiskEncryption = request.Properties.EnabledForDiskEncryption.GetValueOrDefault(false);
        resource.Properties.EnabledForTemplateDeployment = request.Properties.EnabledForTemplateDeployment.GetValueOrDefault(false);
        resource.Properties.EnableSoftDelete = request.Properties.EnableSoftDelete.GetValueOrDefault(true);
        resource.Properties.EnablePurgeProtection = request.Properties.EnablePurgeProtection.GetValueOrDefault(false);
        resource.Properties.EnableRbacAuthorization = request.Properties.EnableRbacAuthorization.GetValueOrDefault(false);
        resource.Properties.SoftDeleteRetentionInDays = request.Properties.SoftDeleteRetentionInDays.GetValueOrDefault(90);
    }
}