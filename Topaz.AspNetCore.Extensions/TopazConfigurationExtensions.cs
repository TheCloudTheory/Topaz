using Azure;
using Azure.Core;
using Azure.Data.AppConfiguration;
using Azure.ResourceManager.AppConfiguration;
using Azure.ResourceManager.AppConfiguration.Models;
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
    /// <param name="objectId">Object ID of the principal performing the operation</param>
    /// <returns>A new TopazEnvironmentBuilder instance configured with the default subscription.</returns>
    public static TopazEnvironmentBuilder AddTopaz(this IConfigurationBuilder builder, Guid defaultSubscriptionId, string objectId)
    {
        return new TopazEnvironmentBuilder(defaultSubscriptionId, objectId);
    }

    /// <summary>
    /// Creates a new Azure subscription with the specified ID and name.
    /// </summary>
    /// <param name="builder">The TopazEnvironmentBuilder instance.</param>
    /// <param name="subscriptionId">The GUID for the new subscription.</param>
    /// <param name="subscriptionName">The display name for the new subscription.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
    public static async Task<TopazEnvironmentBuilder> AddSubscription(this TopazEnvironmentBuilder builder,
        Guid subscriptionId, string subscriptionName, AzureLocalCredential credentials)
    {
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        
        return builder;
    }

    /// <param name="builder">The task containing the TopazEnvironmentBuilder instance.</param>
    extension(Task<TopazEnvironmentBuilder> builder)
    {
        /// <summary>
        /// Creates or updates a resource group in the specified subscription.
        /// </summary>
        /// <param name="subscriptionId">The GUID of the subscription where the resource group will be created.</param>
        /// <param name="resourceGroupName">The name of the resource group to create or update.</param>
        /// <param name="location">The location of the resource group</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
        public async Task<TopazEnvironmentBuilder> AddResourceGroup(Guid subscriptionId, string resourceGroupName, AzureLocation location)
        {
            var concrete = await builder;
            var subscription = await concrete.ArmClient.GetDefaultSubscriptionAsync();
            var resourceGroups = subscription.GetResourceGroups();

            _ = await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName,
                new ResourceGroupData(location));

            return concrete;
        }

        /// <summary>
        /// Creates or updates an Azure Key Vault in the specified resource group.
        /// </summary>
        /// <param name="resourceGroupIdentifier">The resource group where the Key Vault will be created.</param>
        /// <param name="keyVaultName">The name of the Key Vault to create or update.</param>
        /// <param name="operation">The Key Vault creation or update configuration.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
        public async Task<TopazEnvironmentBuilder> AddKeyVault(ResourceGroupIdentifier resourceGroupIdentifier, string keyVaultName, KeyVaultCreateOrUpdateContent operation)
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
        /// <param name="resourceGroupIdentifier">The resource group where the Key Vault will be created.</param>
        /// <param name="keyVaultName">The name of the Key Vault to create or update.</param>
        /// <param name="operation">The Key Vault creation or update configuration.</param>
        /// <param name="secrets">A dictionary of secret names and values to store in the Key Vault.</param>
        /// <param name="objectId">Object ID of the principal performing the operation</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
        /// <remarks>This method first creates the Key Vault, then adds all the specified secrets to it.</remarks>
        public async Task<TopazEnvironmentBuilder> AddKeyVault(ResourceGroupIdentifier resourceGroupIdentifier, string keyVaultName,
            KeyVaultCreateOrUpdateContent operation, IDictionary<string, string> secrets, string objectId)
        {
            await builder.AddKeyVault(resourceGroupIdentifier, keyVaultName, operation);

            var concrete = await builder;
            var credentials = new AzureLocalCredential(objectId);
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
        /// <param name="resourceGroupIdentifier">The resource group where the Storage Account will be created.</param>
        /// <param name="storageAccountName">The name of the Storage Account to create or update.</param>
        /// <param name="operation">The Storage Account creation or update configuration.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
        public async Task<TopazEnvironmentBuilder> AddStorageAccount(ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, StorageAccountCreateOrUpdateContent operation)
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
        /// <param name="resourceGroupIdentifier">The resource group containing the storage account and key vault.</param>
        /// <param name="storageAccountName">The name of the storage account to get the connection string for.</param>
        /// <param name="keyVaultName">The name of the Key Vault where the secret will be stored.</param>
        /// <param name="secretName">The name of the secret that will contain the connection string.</param>
        /// <param name="objectId">Object ID of the principal performing the operation</param>
        /// <param name="keyIndex">The index of the storage account key to use (default is 0 for primary key).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
        /// <remarks>
        /// This method retrieves the storage account keys, selects the key at the specified index,
        /// generates a connection string using TopazResourceHelpers, and stores it as a Key Vault secret.
        /// </remarks>
        public async Task<TopazEnvironmentBuilder> AddStorageAccountConnectionStringAsSecret(ResourceGroupIdentifier resourceGroupIdentifier,
            string storageAccountName, string keyVaultName, string secretName, string objectId, ushort keyIndex = 0)
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

            var credentials = new AzureLocalCredential(objectId);
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
        /// <param name="resourceGroupIdentifier">The resource group where the Service Bus namespace will be created.</param>
        /// <param name="namespaceIdentifier">The Service Bus namespace where the queue will be created.</param>
        /// <param name="data">The Service Bus namespace configuration data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
        public async Task<TopazEnvironmentBuilder> AddServiceBusNamespace(ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier namespaceIdentifier, ServiceBusNamespaceData data)
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
        /// <param name="resourceGroupIdentifier">The resource group containing the Service Bus namespace.</param>
        /// <param name="namespaceIdentifier">The Service Bus namespace where the queue will be created.</param>
        /// <param name="queueName">The name of the Service Bus queue to create or update.</param>
        /// <param name="data">The Service Bus queue configuration data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
        public async Task<TopazEnvironmentBuilder> AddServiceBusQueue(ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier namespaceIdentifier, string queueName, ServiceBusQueueData data)
        {
            var concrete = await builder;
            var subscription = await concrete.ArmClient.GetDefaultSubscriptionAsync();
            var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupIdentifier.Value);
            var @namespace = await resourceGroup.Value.GetServiceBusNamespaceAsync(namespaceIdentifier.Value);

            _ = await @namespace.Value.GetServiceBusQueues().CreateOrUpdateAsync(WaitUntil.Completed, queueName, data);
        
            return concrete;
        }

        /// <summary>
        /// Creates or updates an Azure Service Bus topic within the specified Service Bus namespace.
        /// </summary>
        /// <param name="resourceGroupIdentifier">The resource group containing the Service Bus namespace.</param>
        /// <param name="namespaceIdentifier">The Service Bus namespace where the topic will be created.</param>
        /// <param name="topicName">The name of the Service Bus topic to create or update.</param>
        /// <param name="data">The Service Bus topic configuration data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
        public async Task<TopazEnvironmentBuilder> AddServiceBusTopic(ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier namespaceIdentifier, string topicName, ServiceBusTopicData data)
        {
            var concrete = await builder;
            var subscription = await concrete.ArmClient.GetDefaultSubscriptionAsync();
            var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupIdentifier.Value);
            var @namespace = await resourceGroup.Value.GetServiceBusNamespaceAsync(namespaceIdentifier.Value);

            _ = await @namespace.Value.GetServiceBusTopics().CreateOrUpdateAsync(WaitUntil.Completed, topicName, data);
        
            return concrete;
        }

        /// <summary>
        /// Creates or updates an Azure App Configuration store in the specified resource group.
        /// </summary>
        /// <param name="resourceGroupIdentifier">The resource group where the App Configuration store will be created.</param>
        /// <param name="storeName">The name of the App Configuration store to create or update.</param>
        /// <param name="data">The App Configuration store configuration data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
        public async Task<TopazEnvironmentBuilder> AddConfigurationStore(
            ResourceGroupIdentifier resourceGroupIdentifier, string storeName, AppConfigurationStoreData data)
        {
            var concrete = await builder;
            var subscription = await concrete.ArmClient.GetDefaultSubscriptionAsync();
            var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupIdentifier.Value);
            var stores = resourceGroup.Value.GetAppConfigurationStores();
            
            _ = await stores.CreateOrUpdateAsync(WaitUntil.Completed, storeName, data);

            return concrete;
        }

        /// <summary>
        /// Creates or updates a replica for an existing Azure App Configuration store.
        /// </summary>
        /// <param name="resourceGroupIdentifier">The resource group containing the App Configuration store.</param>
        /// <param name="storeName">The name of the App Configuration store to add a replica to.</param>
        /// <param name="replicaName">The name of the replica to create or update.</param>
        /// <param name="data">The App Configuration replica configuration data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
        public async Task<TopazEnvironmentBuilder> AddConfigurationStoreReplica(
            ResourceGroupIdentifier resourceGroupIdentifier, string storeName, string replicaName, AppConfigurationReplicaData data)
        {
            var concrete = await builder;
            var subscription = await concrete.ArmClient.GetDefaultSubscriptionAsync();
            var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupIdentifier.Value);
            var store = await resourceGroup.Value.GetAppConfigurationStoreAsync(storeName);

            _ = await store.Value.GetAppConfigurationReplicas()
                .CreateOrUpdateAsync(WaitUntil.Completed, replicaName, data);
            
            return concrete;
        }

        /// <summary>
        /// Adds or updates a key-value setting in the specified Azure App Configuration store.
        /// </summary>
        /// <param name="resourceGroupIdentifier">The resource group containing the App Configuration store.</param>
        /// <param name="storeName">The name of the App Configuration store.</param>
        /// <param name="keyName">The key of the configuration setting to add or update.</param>
        /// <param name="value">The value of the configuration setting.</param>
        /// <param name="label">An optional label to associate with the configuration setting.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated TopazEnvironmentBuilder.</returns>
        public async Task<TopazEnvironmentBuilder> AddKeyValuesToStore(
            ResourceGroupIdentifier resourceGroupIdentifier, string storeName, string keyName, string value, string? label = null)
        {
            var concrete = await builder;
            var subscription = await concrete.ArmClient.GetDefaultSubscriptionAsync();
            var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupIdentifier.Value);
            var store = await resourceGroup.Value.GetAppConfigurationStoreAsync(storeName);
            
            var keys = new List<AppConfigurationStoreApiKey>();
            await foreach (var key in store.Value.GetKeysAsync())
                keys.Add(key);
            var connectionString = keys.Single(k => k.Id == "Primary").ConnectionString!;
            var options = new ConfigurationClientOptions
            {
                Retry =
                {
                    MaxRetries = 0
                }
            };
            var configurationClient = new ConfigurationClient(connectionString, options);

            _ = await configurationClient.SetConfigurationSettingAsync(keyName, value, label);
            return concrete;
        }
    }
}