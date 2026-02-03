using System.Text.Json;
using System.Xml.Serialization;
using Azure.Core;
using Azure.Data.Tables.Models;
using Azure.ResourceManager.Storage.Models;
using Topaz.ResourceManager;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Shared;
using TableAnalyticsLoggingSettings = Topaz.Service.Storage.Models.TableAnalyticsLoggingSettings;
using TableMetrics = Topaz.Service.Storage.Models.TableMetrics;
using TableServiceProperties = Topaz.Service.Storage.Models.TableServiceProperties;

namespace Topaz.Service.Storage;

internal sealed class AzureStorageControlPlane(ResourceProvider provider, ITopazLogger logger)
{
    public (OperationResult result, StorageAccountResource? resource) Get(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        var storageAccount = provider.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (string.IsNullOrEmpty(storageAccount))
        {
            return (OperationResult.NotFound, null);
        }
        
        var resource = JsonSerializer.Deserialize<StorageAccountResource>(storageAccount, GlobalSettings.JsonOptions);

        return resource == null ? (OperationResult.Failed, null) : (OperationResult.Success, resource);
    }

    public (OperationResult result, StorageAccountResource? resource) Create(string storageAccountName,
        ResourceGroupIdentifier resourceGroupIdentifier, AzureLocation location,
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var storageAccount = provider.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (!string.IsNullOrEmpty(storageAccount))
        {
            logger.LogError($"Storage account '{storageAccountName}' already exists.");
            return (OperationResult.Failed, null);
        }

        var sku = new ResourceSku
        {
            Name = StorageSkuName.StandardLrs.ToString()
        };
        var properties = new StorageAccountProperties();
        var resource = new StorageAccountResource(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
            location, sku, StorageKind.StorageV2.ToString(), properties);

        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, resource);

        InitializeServicePropertiesFiles(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);

        return (OperationResult.Created, resource);
    }

    private void InitializeServicePropertiesFiles(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        const string propertiesFile = $"properties.xml";
        var propertiesFilePath = Path.Combine(provider.GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName), propertiesFile);

        logger.LogDebug(nameof(AzureStorageControlPlane), nameof(InitializeServicePropertiesFiles), "Attempting to create {0} file.", propertiesFilePath);
        
        if (!File.Exists(propertiesFilePath))
        {
            var logging = new TableAnalyticsLoggingSettings("1.0", true, true, true, new TableRetentionPolicy(true)
            {
                Days = 7
            });
            var metrics = new TableMetrics(true);
            var properties = new TableServiceProperties(logging, metrics, metrics, new List<TableCorsRule>()
            {
                new("http://localhost", "*", "*", "*", 500)
            });
            
            using var sw = new StreamWriter(propertiesFilePath);
            var serializer = new XmlSerializer(typeof(TableServiceProperties));
            serializer.Serialize(sw, properties);
        }
        else
        {
            logger.LogDebug(nameof(AzureStorageControlPlane), nameof(InitializeServicePropertiesFiles), "Attempting to create {0} file - skipped.", propertiesFilePath);
        }
    }

    internal void Delete(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
    }

    public string GetServiceInstancePath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        return provider.GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
    }

    public (OperationResult result, StorageAccountResource resource) CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName,
        CreateOrUpdateStorageAccountRequest request)
    {
        var existingAccount = provider.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        var resource = new StorageAccountResource(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, request.Location!,
            request.Sku!, request.Kind!, request.Properties!);

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, resource);

        InitializeServicePropertiesFiles(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);

        return (string.IsNullOrWhiteSpace(existingAccount) ? OperationResult.Created : OperationResult.Updated,
            resource);
    }
}
