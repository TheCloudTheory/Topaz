using Azure.Core;
using Topaz.Dns;
using Topaz.Service.KeyVault.Models;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.KeyVault.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.KeyVault;

internal sealed class KeyVaultControlPlane(ResourceProvider provider)
{
    public Models.KeyVault Create(string name, ResourceGroupIdentifier resourceGroupIdentifier, AzureLocation location, SubscriptionIdentifier subscriptionIdentifier)
    {
        var model = new Models.KeyVault(name, resourceGroupIdentifier.Value, location, subscriptionIdentifier.Value.ToString());

        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, name, model);

        return model;
    }

    public (OperationResult result, KeyVaultResource resource) CreateOrUpdate(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string keyVaultName, CreateOrUpdateKeyVaultRequest request)
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

        var resource = new KeyVaultResource(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName, request.Location, properties);
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName, resource);

        // This operation must also support handling operation result when Key Vault was updated
        return (OperationResult.Created, resource);
    }

    public (OperationResult result, KeyVaultResource? resource) Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string keyVaultName)
    {
        var resource = provider.GetAs<KeyVaultResource>(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName);
        return resource == null ? (OperationResult.Failed, null) : (OperationResult.Created, resource);
    }

    public (OperationResult result, CheckNameResponse response) CheckName(SubscriptionIdentifier subscriptionIdentifier, string keyVaultName, string? resourceType)
    {
        var dnsEntry = GlobalDnsEntries.GetEntry(KeyVaultService.UniqueName, keyVaultName);
        if (dnsEntry == null)
        {
            return (OperationResult.Success, new CheckNameResponse {  NameAvailable = true });
        }
        
        var keyVault = provider.GetAs<KeyVaultResource>(subscriptionIdentifier, ResourceGroupIdentifier.From(dnsEntry.Value.resourceGroup), keyVaultName);
        if (keyVault == null)
        {
            return (OperationResult.Success, new CheckNameResponse {  NameAvailable = true });
        }

        // If resource type is provided (which can be sent as Azure resource type, i.e. "Microsoft.KeyVault/vaults"),
        // we need to check if the existing Key Vault is of the same type
        if (!string.IsNullOrEmpty(resourceType) && keyVault.Type != resourceType)
        {
            return (OperationResult.Success, new CheckNameResponse { NameAvailable = true });
        }
        
        return (OperationResult.Success, new CheckNameResponse { NameAvailable = false });
    }
    
    public (OperationResult result, KeyVaultResource?[]? resource) ListBySubscription(SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<KeyVaultResource>(subscriptionIdentifier, null, null, 8);

        var filteredResources = resources.Where(resource => resource != null && resource.Id.Contains(subscriptionIdentifier.Value.ToString()));
        return  (OperationResult.Success, filteredResources.ToArray());
    }

    public void Delete(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string keyVaultName)
    {
        var resource = provider.GetAs<KeyVaultResource>(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName);
        if (resource == null)
        {
            return;
        }

        if (!resource.Id.Contains(subscriptionIdentifier.Value.ToString()))
        {
            return;
        }
        
        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName);
    }
}
