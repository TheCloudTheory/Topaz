using Azure.Local.Service.Storage.Models;
using Azure.Local.Shared;

namespace Azure.Local.Service.Storage;

internal sealed class ResourceProvider(ILogger logger)
{
    private readonly ILogger logger = logger;

    public StorageAccount Create(string name)
    {
        var accountPath = Path.Combine(AzureStorageService.LocalDirectoryPath, name);
        if(Directory.Exists(accountPath)) 
        {
            this.logger.LogDebug($"The storage account '{name}' already exists, no changes applied.");
            return new StorageAccount(name);
        }

        this.logger.LogDebug($"Creating storage account '{name}'.");
        Directory.CreateDirectory(accountPath);

        return new StorageAccount(name);
    }

    internal bool CheckIfStorageAccountExists(string name)
    {
        var accountPath = Path.Combine(AzureStorageService.LocalDirectoryPath, name);
        return Directory.Exists(accountPath);
    }
}
