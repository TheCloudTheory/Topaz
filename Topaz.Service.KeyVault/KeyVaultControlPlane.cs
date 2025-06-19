using System.Text.Json;
using Topaz.ResourceManager;
using Topaz.Service.KeyVault.Models;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

internal sealed class KeyVaultControlPlane(ResourceProvider provider)
{
    public Models.KeyVault Create(string name, string resourceGroup, string location, string subscriptionId)
    {
        var model = new Models.KeyVault(name, resourceGroup, location, subscriptionId);

        provider.Create(name, model);

        return model;
    }
    
    public (OperationResult result, KeyVaultResource resource) CreateOrUpdate(string subscriptionId, string resourceGroupName, string keyVaultName, CreateOrUpdateKeyVaultRequest request)
    {
        var properties = new KeyVaultResourceProperties
        {
            Sku = new KeyVaultResourceProperties.KeyVaultSku()
            {
                Family = request.Properties.Sku.Family,
                Name = request.Properties.Sku.Name
            },
            TenantId = request.Properties.TenantId
        };
        
        var resource = new KeyVaultResource(subscriptionId, resourceGroupName, keyVaultName, request.Location, properties);
        provider.CreateOrUpdate(keyVaultName, resource);

        // This operation must also support handling operation result when Key Vault was updated
        return (OperationResult.Created, resource);
    }

    public (OperationResult result, KeyVaultResource? resource) Get(string keyVaultName)
    {
        var resource = provider.GetAs<KeyVaultResource>(keyVaultName);
        return resource == null ? (OperationResult.Failed, null) : (OperationResult.Created, resource);
    }
}
