using Azure.Local.Service.Storage.Models;
using Azure.Local.Shared;

namespace Azure.Local.Service.Storage;

internal sealed class ResourceProvider
{
    public StorageAccount Create(string name)
    {
        var accountPath = Path.Combine(AzureStorageService.LocalDirectoryPath, name);
        if(Directory.Exists(accountPath)) 
        {
            PrettyLogger.LogDebug($"The storage account '{name}' already exists, no changes applied.");
            return new StorageAccount(name);
        }

        Directory.CreateDirectory(accountPath);

        return new StorageAccount(name);
    }
}
