using System.Text.Json;
using System.Xml.Serialization;
using Azure.Data.Tables.Models;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Shared;
using TableAnalyticsLoggingSettings = Topaz.Service.Storage.Models.TableAnalyticsLoggingSettings;
using TableMetrics = Topaz.Service.Storage.Models.TableMetrics;
using TableServiceProperties = Topaz.Service.Storage.Models.TableServiceProperties;

namespace Topaz.Service.Storage;

internal sealed class AzureStorageControlPlane(ResourceProvider provider, ILogger logger)
{
    public (OperationResult result, StorageAccountResource? resource) Get(string keyVaultName)
    {
        var content = provider.Get(keyVaultName);
        if (string.IsNullOrEmpty(content))
        {
            return (OperationResult.Failed, null);
        }
        
        var resource = JsonSerializer.Deserialize<StorageAccountResource>(content, GlobalSettings.JsonOptions);

        return resource == null ? (OperationResult.Failed, null) : (OperationResult.Created, resource);
    }

    public StorageAccount Create(string name, string resourceGroup, string location, string subscriptionId)
    {
        if(CheckIfStorageAccountExists(resourceGroup) == false)
        {
            throw new InvalidOperationException();
        }

        var model = new StorageAccount(name, resourceGroup, location, subscriptionId);

        provider.Create(name, model);
        
        InitializeServicePropertiesFiles(name);

        return model;
    }

    private void InitializeServicePropertiesFiles(string storageAccountName)
    {
        var propertiesFile = $"properties.xml";
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
    private bool CheckIfStorageAccountExists(string resourceGroup)
    {
        var rp = new ResourceGroupControlPlane(new ResourceGroup.ResourceProvider(logger));
        var data = rp.Get(resourceGroup);

        return data != null;
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
        
        return (string.IsNullOrWhiteSpace(existingAccount) ? OperationResult.Created : OperationResult.Updated, resource);
    }
}
