using Azure.Core;
using Topaz.Dns;
using Topaz.ResourceManager;
using Topaz.Service.KeyVault.Models;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.KeyVault.Models.Responses;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

internal sealed class KeyVaultControlPlane(
    KeyVaultResourceProvider provider,
    ResourceGroupControlPlane resourceGroupControlPlane,
    SubscriptionControlPlane subscriptionControlPlane,
    ITopazLogger logger) : IControlPlane
{
    private const string KeyVaultNotFoundCode = "KeyVaultNotFound";
    private const string KeyVaultNotFoundMessageTemplate =
        "Key Vault '{0}' could not be found";
    private const string InvalidVaultNameMessageTemplate = "The vault name '{0}' is invalid. A vault's name must be between 3-24 alphanumeric characters. The name must begin with a letter, end with a letter or digit, and not contain consecutive hyphens.";

    public ControlPlaneOperationResult<KeyVaultResource> Create(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, AzureLocation location, string keyVaultName)
    {
        var isNameValid = CheckIfKeyVaultNameIsValid(keyVaultName);
        if (!isNameValid)
        {
            return new ControlPlaneOperationResult<KeyVaultResource>(OperationResult.Failed, null,
                string.Format(InvalidVaultNameMessageTemplate, keyVaultName),
                "VaultNameNotValid");
        }

        var resourceGroupOperation = resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<KeyVaultResource>(OperationResult.Failed, null,
                resourceGroupOperation.Reason,
                resourceGroupOperation.Code);
        }
        
        var resource = new KeyVaultResource(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName, location, null, KeyVaultResourceProperties.Default(keyVaultName));

        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName, resource);

        return new ControlPlaneOperationResult<KeyVaultResource>(OperationResult.Created, resource, null, null);
    }

    public ControlPlaneOperationResult<KeyVaultResource> CreateOrUpdate(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string keyVaultName, CreateOrUpdateKeyVaultRequest request)
    {
        var isNameValid = CheckIfKeyVaultNameIsValid(keyVaultName);
        if (!isNameValid)
        {
            return new ControlPlaneOperationResult<KeyVaultResource>(OperationResult.Failed, null,
                string.Format(InvalidVaultNameMessageTemplate, keyVaultName),
                "VaultNameNotValid");
        }
        
        var resourceGroupOperation = resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<KeyVaultResource>(OperationResult.Failed, null,
                resourceGroupOperation.Reason,
                resourceGroupOperation.Code);
        }
        
        var properties = KeyVaultResourceProperties.FromRequest(keyVaultName, request);
        var resource = new KeyVaultResource(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName, request.Location!, request.Tags, properties);
        
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName, resource);

        // This operation must also support handling operation result when Key Vault was updated
        return new ControlPlaneOperationResult<KeyVaultResource>(OperationResult.Created, resource, null, null);
    }

    private bool CheckIfKeyVaultNameIsValid(string keyVaultName)
    {
        const int minLength = 3;
        const int maxLength = 24;
        
        if (keyVaultName.Length is < minLength or > maxLength) return false;
        if(!char.IsLetter(keyVaultName[0])) return false;
        if(!char.IsLetter(keyVaultName[^1]) && !char.IsDigit(keyVaultName[^1])) return false;

        var characters = keyVaultName.Select(c => c).ToArray();
        if(characters.Any(c => !char.IsDigit(c) && !char.IsLetter(c) && c != '-')) return false;

        return !characters.Where((t, index) => index != 0 && (t == '-' && characters[index - 1] == '-')).Any();
    }

    public ControlPlaneOperationResult<KeyVaultResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string keyVaultName)
    {
        var resource = provider.GetAs<KeyVaultResource>(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName);
        return resource == null && GlobalDnsEntries.IsSoftDeleted(KeyVaultService.UniqueName, keyVaultName) ? 
            new ControlPlaneOperationResult<KeyVaultResource>(OperationResult.NotFound, null, string.Format(KeyVaultNotFoundMessageTemplate, keyVaultName), KeyVaultNotFoundCode) 
            : new ControlPlaneOperationResult<KeyVaultResource>(OperationResult.Created, resource, null, null);
    }

    public (OperationResult result, CheckNameResponse response) CheckName(SubscriptionIdentifier subscriptionIdentifier, string keyVaultName, string? resourceType)
    {
        var isNameValid = CheckIfKeyVaultNameIsValid(keyVaultName);
        if (!isNameValid)
        {
            return (OperationResult.Success,
                new CheckNameResponse
                {
                    NameAvailable = false, Reason = CheckNameResponse.NoAvailabilityReason.AccountNameInvalid,
                    Message = string.Format(InvalidVaultNameMessageTemplate, keyVaultName)
                });
        }
        
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

        return (OperationResult.Success,
            new CheckNameResponse
            {
                NameAvailable = false, Reason = CheckNameResponse.NoAvailabilityReason.AlreadyExists,
                Message = $"The name '{keyVaultName}' is already in use."
            });
    }
    
    public (OperationResult result, KeyVaultResource?[]? resource) ListBySubscription(SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<KeyVaultResource>(subscriptionIdentifier, null, null, 8);

        var filteredResources = resources.Where(resource => resource.IsInSubscription(subscriptionIdentifier));
        return  (OperationResult.Success, filteredResources.ToArray());
    }

    public void Delete(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string keyVaultName)
    {
        var resource = provider.GetAs<KeyVaultResource>(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName);
        if (resource == null)
        {
            return;
        }

        if (!resource.IsInSubscription(subscriptionIdentifier))
        {
            return;
        }
        
        // Note that Azure Key Vault is soft deleted
        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName, true);
    }

    public OperationResult Deploy(GenericResource resource)
    {
        var keyVault = resource.As<KeyVaultResource, KeyVaultResourceProperties>();
        if (keyVault == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Key Vault instance.");
            return OperationResult.Failed;
        }

        var result = CreateOrUpdate(keyVault.GetSubscription(), keyVault.GetResourceGroup(), keyVault.Name,
            new CreateOrUpdateKeyVaultRequest
            {
                Location = keyVault.Location,
                Properties = new CreateOrUpdateKeyVaultRequest.KeyVaultProperties
                {
                    Sku = new CreateOrUpdateKeyVaultRequest.KeyVaultProperties.KeyVaultSku
                    {
                        Name = keyVault.Sku!.Name,
                        Family = keyVault.Sku.Family
                    },
                    TenantId = keyVault.Properties.TenantId
                }
            });

        return result.Result;
    }

    public (OperationResult result, KeyVaultResource?[]? resource) ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var subscription = subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscription.Resource == null || subscription.Result == OperationResult.NotFound)
        {
            return (OperationResult.NotFound, null);
        }

        var resourceGroup = resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroup.Resource == null || resourceGroup.Result == OperationResult.NotFound)
        {
            return (OperationResult.NotFound, null);
        }

        var resources = provider.ListAs<KeyVaultResource>(subscriptionIdentifier, null, null, 8);
        var filteredResources = resources.Where(resource =>
            resource.IsInSubscription(subscriptionIdentifier) && resource.IsInResourceGroup(resourceGroupIdentifier));
        
        return  (OperationResult.Success, filteredResources.ToArray());
    }
}
