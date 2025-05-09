using System.Text.Json;
using Azure.Local.Service.Shared;
using Azure.Local.Service.Storage.Models;
using Azure.Local.Shared;

namespace Azure.Local.Service.Storage;

internal sealed class ResourceProvider(ILogger logger)
{
    private readonly ILogger logger = logger;

    public StorageAccount Create(string name, string resourceGroup, string location)
    {
        var accountPath = Path.Combine(AzureStorageService.LocalDirectoryPath, name);
        if(Directory.Exists(accountPath)) 
        {
            this.logger.LogDebug($"The storage account '{name}' already exists, no changes applied.");

            var metadata = GetMetadataFile(accountPath);
            return metadata;
        }

        this.logger.LogDebug($"Creating storage account '{name}'.");

        if(CheckIfResourceGroupExists(resourceGroup) == false)
        {
            throw new InvalidOperationException();
        }

        Directory.CreateDirectory(accountPath);

        var storageAccount = new StorageAccount(name, resourceGroup, location);

        AddMetadataFile(storageAccount, accountPath);

        return storageAccount;
    }

    private StorageAccount GetMetadataFile(string accountPath)
    {
        var filePath = Path.Combine(accountPath, "metadata.json");
        var content = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<StorageAccount>(content, GlobalSettings.JsonOptions) ??
         throw new InvalidOperationException();
        return data;
    }

    private bool CheckIfResourceGroupExists(string resourceGroup)
    {
        var rp = new ResourceGroup.ResourceProvider(this.logger);
        var (data, _) = rp.Get(resourceGroup);

        return data != null;
    }

    private void AddMetadataFile(StorageAccount data, string accountPath)
    {
        var filePath = Path.Combine(accountPath, "metadata.json");
        var content = JsonSerializer.Serialize(data, GlobalSettings.JsonOptions);

        File.WriteAllText(filePath, content);
    }

    internal bool CheckIfStorageAccountExists(string name)
    {
        var accountPath = Path.Combine(AzureStorageService.LocalDirectoryPath, name);
        return Directory.Exists(accountPath);
    }

    internal void Delete(string name)
    {
        var accountPath = Path.Combine(AzureStorageService.LocalDirectoryPath, name);
        if(Directory.Exists(accountPath) == false) 
        {
            this.logger.LogDebug($"The storage account '{name}' does not exists, no changes applied.");
            return;
        }

        this.logger.LogDebug($"Deleting storage account '{name}'.");
        Directory.Delete(accountPath, true);

        return;
    }
}
