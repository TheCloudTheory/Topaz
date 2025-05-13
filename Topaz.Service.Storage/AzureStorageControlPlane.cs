using System.Text.Json;
using System.Xml.Serialization;
using Azure.Data.Tables.Models;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Shared;
using TableAnalyticsLoggingSettings = Topaz.Service.Storage.Models.TableAnalyticsLoggingSettings;
using TableMetrics = Topaz.Service.Storage.Models.TableMetrics;
using TableServiceProperties = Topaz.Service.Storage.Models.TableServiceProperties;

namespace Topaz.Service.Storage;

internal sealed class AzureStorageControlPlane(ResourceProvider provider, ILogger logger)
{
    private readonly ResourceProvider provider = provider;
    private readonly ILogger logger = logger;

    public Models.StorageAccount Get(string name)
    {
        var data = this.provider.Get(name);
        var model = JsonSerializer.Deserialize<Models.StorageAccount>(data, GlobalSettings.JsonOptions);

        return model!;
    }

    public Models.StorageAccount Create(string name, string resourceGroup, string location, string subscriptionId)
    {
        if(CheckIfStorageAccountExists(resourceGroup) == false)
        {
            throw new InvalidOperationException();
        }

        var model = new StorageAccount(name, resourceGroup, location, subscriptionId);

        this.provider.Create(name, model);
        
        InitializeServicePropertiesFiles(name);

        return model;
    }

    private void InitializeServicePropertiesFiles(string storageAccountName)
    {
        var propertiesFile = $"properties.xml";
        var propertiesFilePath = Path.Combine(this.provider.GetServiceInstancePath(storageAccountName), propertiesFile);

        this.logger.LogDebug($"Attempting to create {propertiesFilePath} file.");
        
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
            this.logger.LogDebug($"Attempting to create {propertiesFilePath} file - skipped.");
        }
    }
    private bool CheckIfStorageAccountExists(string resourceGroup)
    {
        var rp = new ResourceGroupControlPlane(new ResourceGroup.ResourceProvider(this.logger));
        var data = rp.Get(resourceGroup);

        return data != null;
    }

    internal void Delete(string storageAccountName)
    {
        this.provider.Delete(storageAccountName);
    }

    public string GetServiceInstancePath(string storageAccountName)
    {
        return this.provider.GetServiceInstancePath(storageAccountName);
    }
}
