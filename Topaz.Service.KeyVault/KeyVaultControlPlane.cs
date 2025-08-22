using Azure.Core;
using Topaz.Service.KeyVault.Models;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.KeyVault.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.KeyVault;

internal sealed class KeyVaultControlPlane(ResourceProvider provider)
{
    public Models.KeyVault Create(string name, ResourceGroupIdentifier resourceGroup, AzureLocation location, SubscriptionIdentifier subscriptionId)
    {
        var model = new Models.KeyVault(name, resourceGroup.Value, location, subscriptionId.Value.ToString());

        provider.Create(name, model);

        return model;
    }

    public (OperationResult result, KeyVaultResource resource) CreateOrUpdate(SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup, string keyVaultName, CreateOrUpdateKeyVaultRequest request)
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

        var resource = new KeyVaultResource(subscriptionId, resourceGroup, keyVaultName, request.Location, properties);
        provider.CreateOrUpdate(keyVaultName, resource);

        // This operation must also support handling operation result when Key Vault was updated
        return (OperationResult.Created, resource);
    }

    public (OperationResult result, KeyVaultResource? resource) Get(string keyVaultName)
    {
        var resource = provider.GetAs<KeyVaultResource>(keyVaultName);
        return resource == null ? (OperationResult.Failed, null) : (OperationResult.Created, resource);
    }

    public (OperationResult result, CheckNameResponse response) CheckName(string keyVaultName, string? resourceType)
    {
        var keyVault = provider.GetAs<KeyVaultResource>(keyVaultName);
        if (keyVault == null)
        {
            return (OperationResult.Success, new CheckNameResponse {  NameAvailable = true });
        }

        // If resource type is provided (which can be sent as Azure resource type, i.e. "Microsoft.KeyVault/vaults"),
        // we need to check if the existing Key Vault is of the same type
        if (string.IsNullOrEmpty(resourceType) == false && keyVault.Type != resourceType)
        {
            return (OperationResult.Success, new CheckNameResponse {  NameAvailable = true });
        }
        
        return (OperationResult.Success, new CheckNameResponse { NameAvailable = false });
    }
}
