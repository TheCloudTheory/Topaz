using Azure;
using Azure.Core;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.ServiceBus;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.AspNetCore.Extensions;

public static class TopazConfigurationExtensions
{
    /// <summary>
    /// Initializes a new TopazEnvironmentBuilder with the specified default subscription ID.
    /// </summary>
    /// <param name="builder">The configuration builder instance.</param>
    /// <param name="defaultSubscriptionId">The GUID of the default Azure subscription to use.</param>
    /// <returns>A new TopazEnvironmentBuilder instance configured with the default subscription.</returns>
    public static TopazEnvironmentBuilder AddTopaz(this IConfigurationBuilder builder, Guid defaultSubscriptionId)
    {
        return new TopazEnvironmentBuilder(defaultSubscriptionId);
    }

    /// <summary>
    /// Creates a new Azure subscription with the specified ID and name.
    /// </summary>
    /// <param name="builder">The TopazEnvironmentBuilder instance.</param>
    /// <param name="subscriptionId">The GUID for the new subscription.</param>
    /// <param name="subscriptionName">The display name for the new subscription.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
    public static async Task<TopazEnvironmentBuilder> AddSubscription(this TopazEnvironmentBuilder builder,
        Guid subscriptionId, string subscriptionName)
    {
        using var topaz = new TopazArmClient();
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        
        return builder;
    }

    /// <summary>
    /// Creates or updates a resource group in the specified subscription.
    /// </summary>
    /// <param name="builder">The task containing the TopazEnvironmentBuilder instance.</param>
    /// <param name="subscriptionId">The GUID of the subscription where the resource group will be created.</param>
    /// <param name="resourceGroupName">The name of the resource group to create or update.</param>
    /// <param name="location">The location of the resource group</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
    public static async Task<TopazEnvironmentBuilder> AddResourceGroup(this Task<TopazEnvironmentBuilder> builder, Guid subscriptionId, string resourceGroupName, AzureLocation location)
    {
        var concrete = await builder;
        var subscription = await concrete.ArmClient.GetDefaultSubscriptionAsync();
        var resourceGroups = subscription.GetResourceGroups();
        
        _ = await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, new ResourceGroupData(location));

        return concrete;
    }

    /// <summary>
    /// Creates or updates an Azure Key Vault in the specified resource group.
    /// </summary>
    /// <param name="builder">The task containing the TopazEnvironmentBuilder instance.</param>
    /// <param name="resourceGroupIdentifier">The resource group where the Key Vault will be created.</param>
    /// <param name="keyVaultName">The name of the Key Vault to create or update.</param>
    /// <param name="operation">The Key Vault creation or update configuration.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
    public static async Task<TopazEnvironmentBuilder> AddKeyVault(this Task<TopazEnvironmentBuilder> builder, ResourceGroupIdentifier resourceGroupIdentifier, string keyVaultName, KeyVaultCreateOrUpdateContent operation)
    {
        var concrete = await builder;
        var subscription = await concrete.ArmClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupIdentifier.Value);
        
        _ = await resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdateAsync(WaitUntil.Completed, keyVaultName, operation, CancellationToken.None);
        
        return concrete;
    }

    /// <summary>
    /// Creates or updates an Azure Key Vault in the specified resource group and populates it with the provided secrets.
    /// </summary>
    /// <param name="builder">The task containing the TopazEnvironmentBuilder instance.</param>
    /// <param name="resourceGroupIdentifier">The resource group where the Key Vault will be created.</param>
    /// <param name="keyVaultName">The name of the Key Vault to create or update.</param>
    /// <param name="operation">The Key Vault creation or update configuration.</param>
    /// <param name="secrets">A dictionary of secret names and values to store in the Key Vault.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
    /// <remarks>This method first creates the Key Vault, then adds all the specified secrets to it.</remarks>
    public static async Task<TopazEnvironmentBuilder> AddKeyVault(this Task<TopazEnvironmentBuilder> builder, ResourceGroupIdentifier resourceGroupIdentifier, string keyVaultName,
        KeyVaultCreateOrUpdateContent operation, IDictionary<string, string> secrets)
    {
        await builder.AddKeyVault(resourceGroupIdentifier, keyVaultName, operation);
        
        var concrete = await builder;
        var credentials = new AzureLocalCredential();
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(keyVaultName),
            credential: credentials, new SecretClientOptions
            {
                DisableChallengeResourceVerification = true
            });

        foreach (var secret in secrets)
        {
            await client.SetSecretAsync(secret.Key, secret.Value);
        }

        return concrete;
    }

    /// <summary>
    /// Creates or updates an Azure Storage Account in the specified resource group.
    /// </summary>
    /// <param name="builder">The task containing the TopazEnvironmentBuilder instance.</param>
    /// <param name="resourceGroupIdentifier">The resource group where the Storage Account will be created.</param>
    /// <param name="storageAccountName">The name of the Storage Account to create or update.</param>
    /// <param name="operation">The Storage Account creation or update configuration.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
    public static async Task<TopazEnvironmentBuilder> AddStorageAccount(this Task<TopazEnvironmentBuilder> builder,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, StorageAccountCreateOrUpdateContent operation)
    {
        var concrete = await builder;
        var subscription = await concrete.ArmClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupIdentifier.Value);
        
        _ = await resourceGroup.Value.GetStorageAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, storageAccountName, operation);
        
        return concrete;
    }

    /// <summary>
    /// Stores an Azure Storage Account connection string as a secret in the specified Key Vault.
    /// </summary>
    /// <param name="builder">The task containing the TopazEnvironmentBuilder instance.</param>
    /// <param name="resourceGroupIdentifier">The resource group containing the storage account and key vault.</param>
    /// <param name="storageAccountName">The name of the storage account to get the connection string for.</param>
    /// <param name="keyVaultName">The name of the Key Vault where the secret will be stored.</param>
    /// <param name="secretName">The name of the secret that will contain the connection string.</param>
    /// <param name="keyIndex">The index of the storage account key to use (default is 0 for primary key).</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
    /// <remarks>
    /// This method retrieves the storage account keys, selects the key at the specified index,
    /// generates a connection string using TopazResourceHelpers, and stores it as a Key Vault secret.
    /// </remarks>
    public static async Task<TopazEnvironmentBuilder> AddStorageAccountConnectionStringAsSecret(
        this Task<TopazEnvironmentBuilder> builder, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string keyVaultName, string secretName, ushort keyIndex = 0)
    {
        var concrete = await builder;
        var subscription = await concrete.ArmClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupIdentifier.Value);
        var storageAccount = await resourceGroup.Value.GetStorageAccountAsync(storageAccountName);
        var keys = new List<StorageAccountKey>();
        
        await foreach (var key in storageAccount.Value.GetKeysAsync())
        {
            keys.Add(key);
        }
        
        var credentials = new AzureLocalCredential();
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(keyVaultName),
            credential: credentials, new SecretClientOptions
            {
                DisableChallengeResourceVerification = true
            });

        var selectedKey = keys[keyIndex];
        await client.SetSecretAsync(new KeyVaultSecret(secretName,
            TopazResourceHelpers.GetAzureStorageConnectionString(storageAccountName, selectedKey.Value)));
        
        return concrete;
    }

    /// <summary>
    /// Creates or updates an Azure Service Bus namespace in the specified resource group.
    /// </summary>
    /// <param name="builder">The task containing the TopazEnvironmentBuilder instance.</param>
    /// <param name="resourceGroupIdentifier">The resource group where the Service Bus namespace will be created.</param>
    /// <param name="namespaceIdentifier">The Service Bus namespace where the queue will be created.</param>
    /// <param name="data">The Service Bus namespace configuration data.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
    public static async Task<TopazEnvironmentBuilder> AddServiceBusNamespace(this Task<TopazEnvironmentBuilder> builder,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier namespaceIdentifier, ServiceBusNamespaceData data)
    {
        var concrete = await builder;
        var subscription = await concrete.ArmClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupIdentifier.Value);

        _ = await resourceGroup.Value.GetServiceBusNamespaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, namespaceIdentifier.Value, data);
        
        return concrete;
    }
    
    /// <summary>
    /// Creates or updates an Azure Service Bus queue within the specified Service Bus namespace.
    /// </summary>
    /// <param name="builder">The task containing the TopazEnvironmentBuilder instance.</param>
    /// <param name="resourceGroupIdentifier">The resource group containing the Service Bus namespace.</param>
    /// <param name="namespaceIdentifier">The Service Bus namespace where the queue will be created.</param>
    /// <param name="queueName">The name of the Service Bus queue to create or update.</param>
    /// <param name="data">The Service Bus queue configuration data.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
    public static async Task<TopazEnvironmentBuilder> AddServiceBusQueue(this Task<TopazEnvironmentBuilder> builder,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier namespaceIdentifier, string queueName, ServiceBusQueueData data)
    {
        var concrete = await builder;
        var subscription = await concrete.ArmClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupIdentifier.Value);
        var @namespace = await resourceGroup.Value.GetServiceBusNamespaceAsync(namespaceIdentifier.Value);

        _ = await @namespace.Value.GetServiceBusQueues().CreateOrUpdateAsync(WaitUntil.Completed, queueName, data);
        
        return concrete;
    }
}