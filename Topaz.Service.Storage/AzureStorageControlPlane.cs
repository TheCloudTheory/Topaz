using System.Text.Json;
using System.Xml.Serialization;
using Azure.Data.Tables.Models;
using Azure.ResourceManager.Storage.Models;
using Topaz.ResourceManager;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Shared;
using TableAnalyticsLoggingSettings = Topaz.Service.Storage.Models.TableAnalyticsLoggingSettings;
using TableMetrics = Topaz.Service.Storage.Models.TableMetrics;
using TableServiceProperties = Topaz.Service.Storage.Models.TableServiceProperties;

namespace Topaz.Service.Storage;

internal sealed class AzureStorageControlPlane(ResourceProvider provider, ITopazLogger logger)
{
    public (OperationResult result, StorageAccountResource? resource) Get(string storageAccountName)
    {
        var storageAccount = provider.Get(storageAccountName);
        if (string.IsNullOrEmpty(storageAccount))
        {
            return (OperationResult.NotFound, null);
        }
        
        var resource = JsonSerializer.Deserialize<StorageAccountResource>(storageAccount, GlobalSettings.JsonOptions);

        return resource == null ? (OperationResult.Failed, null) : (OperationResult.Success, resource);
    }

    public (OperationResult result, StorageAccountResource? resource) Create(string storageAccountName, string resourceGroupName, string location, string subscriptionId)
    {
        var storageAccount = provider.Get(storageAccountName);
        if (string.IsNullOrEmpty(storageAccount) == false)
        {
            logger.LogError($"Storage account '{storageAccountName}' already exists.");
            return (OperationResult.Failed, null);
        }

        var sku = new ResourceSku
        {
            Name = StorageSkuName.StandardLrs.ToString()
        };
        var properties = new StorageAccountProperties();
        var resource = new StorageAccountResource(subscriptionId, resourceGroupName, storageAccountName, location, sku, StorageKind.StorageV2.ToString(), properties);

        provider.Create(storageAccountName, resource);
        
        InitializeServicePropertiesFiles(storageAccountName);

        return (OperationResult.Created, resource);
    }

    private void InitializeServicePropertiesFiles(string storageAccountName)
    {
        const string propertiesFile = $"properties.xml";
        var propertiesFilePath = Path.Combine(provider.GetServiceInstancePath(storageAccountName), propertiesFile);

        logger.LogDebug($"Attempting to create {propertiesFilePath} file.");
        
        if (File.Exists(propertiesFilePath) == false)
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
            logger.LogDebug($"Attempting to create {propertiesFilePath} file - skipped.");
        }
    }

    internal void Delete(string storageAccountName)
    {
        provider.Delete(storageAccountName);
    }

    public string GetServiceInstancePath(string storageAccountName)
    {
        return provider.GetServiceInstancePath(storageAccountName);
    }
    
    public (OperationResult result, StorageAccountResource resource) CreateOrUpdate(string subscriptionId, string resourceGroupName, string storageAccountName, CreateOrUpdateStorageAccountRequest request)
    {
        var existingAccount = provider.Get(storageAccountName);
        var resource = new StorageAccountResource(subscriptionId, resourceGroupName, storageAccountName, request.Location!, request.Sku!, request.Kind!, request.Properties!);
        
        provider.CreateOrUpdate(storageAccountName, resource);
        
        InitializeServicePropertiesFiles(storageAccountName);
        
        return (string.IsNullOrWhiteSpace(existingAccount) ? OperationResult.Created : OperationResult.Updated, resource);
    }
}
