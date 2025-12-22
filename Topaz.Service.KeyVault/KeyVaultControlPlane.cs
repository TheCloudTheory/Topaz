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
        
        var subscriptionOperation = subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscriptionOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<KeyVaultResource>(OperationResult.Failed, null,
                subscriptionOperation.Reason,
                subscriptionOperation.Code);
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

        KeyVaultFullResource resource;
        var isRecoverMode = request.Properties?.CreateMode == "recover";
        var keyVaultOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName, isRecoverMode);
        var createOperation = keyVaultOperation.Result == OperationResult.NotFound;
        if (createOperation)
        {
            var properties = KeyVaultResourceProperties.FromRequest(keyVaultName, request);
            resource = new KeyVaultFullResource(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName, request.Location!, request.Tags, properties);
        }
        else
        {
            KeyVaultResourceProperties.UpdateFromRequest(keyVaultOperation.Resource!, request);
            resource = keyVaultOperation.Resource!;
            SetRecoverPropertiesForKeyVault(isRecoverMode, resource);
        }
        
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName, resource, createOperation, isRecoverMode);
        
        return new ControlPlaneOperationResult<KeyVaultResource>(
            createOperation ? OperationResult.Created : OperationResult.Updated,
            resource, null, null);
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

        return !characters.Where((t, index) => index != 0 && t == '-' && characters[index - 1] == '-').Any();
    }

    public ControlPlaneOperationResult<KeyVaultFullResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string keyVaultName, bool ignoreSoftDeleted = false)
    {
        var resource = provider.GetAs<KeyVaultFullResource>(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName);
        return resource == null || (GlobalDnsEntries.IsSoftDeleted(KeyVaultService.UniqueName, keyVaultName) && !ignoreSoftDeleted) ? 
            new ControlPlaneOperationResult<KeyVaultFullResource>(OperationResult.NotFound, null, string.Format(KeyVaultNotFoundMessageTemplate, keyVaultName), KeyVaultNotFoundCode) 
            : new ControlPlaneOperationResult<KeyVaultFullResource>(OperationResult.Success, resource, null, null);
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
    
    public (OperationResult result, KeyVaultFullResource?[]? resource) ListBySubscription(SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<KeyVaultFullResource>(subscriptionIdentifier, null, null, 8);

        var filteredResources = resources.Where(resource => resource.IsInSubscription(subscriptionIdentifier));
        return (OperationResult.Success, filteredResources.ToArray());
    }
    
    public (OperationResult result, KeyVaultFullResource?[]? resource) ListDeletedBySubscription(SubscriptionIdentifier subscriptionIdentifier)
    {
        var keyVaults = ListBySubscription(subscriptionIdentifier);
        var filteredResources = keyVaults.resource!.Where(keyVault =>
            GlobalDnsEntries.IsSoftDeleted(KeyVaultService.UniqueName, keyVault!.Name));
        
        return (OperationResult.Success, filteredResources.ToArray());
    }

    public void Delete(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string keyVaultName)
    {
        var resource = provider.GetAs<KeyVaultFullResource>(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName);
        if (resource == null)
        {
            return;
        }

        if (!resource.IsInSubscription(subscriptionIdentifier))
        {
            return;
        }
        
        if (!resource.IsInResourceGroup(resourceGroupIdentifier))
        {
            return;
        }
        
        resource.DeletionDate = DateTimeOffset.Now;
        resource.ScheduledPurgeDate = DateTimeOffset.Now.AddDays(resource.Properties.SoftDeleteRetentionInDays);
        
        // First Key Vault needs to be updated with the additional properties we added
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName, resource);
        
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

    public (OperationResult result, KeyVaultFullResource? resource) ShowDeleted(SubscriptionIdentifier subscriptionIdentifier, string keyVaultName)
    {
        var subscription = subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscription.Resource == null || subscription.Result == OperationResult.NotFound)
        {
            return (OperationResult.NotFound, null);
        }
        
        var keyVaults = ListBySubscription(subscriptionIdentifier);
        var keyVault = keyVaults.resource!.SingleOrDefault(keyVault => keyVault!.Name == keyVaultName);
            GlobalDnsEntries.IsSoftDeleted(KeyVaultService.UniqueName, keyVaultName);

        return keyVault == null ? (OperationResult.NotFound, null) : (OperationResult.Success, keyVault);
    }

    public (OperationResult operationResult, string? vaultUri) Purge(SubscriptionIdentifier subscriptionIdentifier, string location, string keyVaultName)
    {
        var subscription = subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscription.Resource == null || subscription.Result == OperationResult.NotFound)
        {
            return (OperationResult.NotFound, null);
        }

        var keyVault = ShowDeleted(subscriptionIdentifier, keyVaultName);
        if (keyVault.resource == null || keyVault.result == OperationResult.NotFound)
        {
            return (OperationResult.NotFound, null);
        }
        
        provider.Delete(subscriptionIdentifier, keyVault.resource.GetResourceGroup(), keyVaultName);
        return (OperationResult.Success, keyVault.resource.Properties.VaultUri);
    }

    public ControlPlaneOperationResult<KeyVaultFullResource> Update(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string keyVaultName, UpdateKeyVaultRequest request)
    {
        var resourceGroupOperation = resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<KeyVaultFullResource>(OperationResult.Failed, null,
                resourceGroupOperation.Reason,
                resourceGroupOperation.Code);
        }

        var isRecoverMode = request.Properties?.CreateMode == "recover";
        var keyVaultOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName, isRecoverMode);
        if (keyVaultOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<KeyVaultFullResource>(OperationResult.NotFound, null,
                string.Format(KeyVaultNotFoundMessageTemplate, keyVaultName), KeyVaultNotFoundCode);
        }
        
        var resource = keyVaultOperation.Resource!;

        SetRecoverPropertiesForKeyVault(isRecoverMode, resource);

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName, resource, false, isRecoverMode);
        
        return new ControlPlaneOperationResult<KeyVaultFullResource>(
            OperationResult.Updated,
            resource, null, null);
    }

    /// <summary>
    /// If the provided PATCH request is referring to a recover operation, we need to get
    /// rid of the properties indicating, that it was removed
    /// </summary>
    private static void SetRecoverPropertiesForKeyVault(bool isRecoverMode, KeyVaultFullResource resource)
    {
        if (!isRecoverMode) return;
        
        resource.DeletionDate = null;
        resource.ScheduledPurgeDate = null;
    }
}
