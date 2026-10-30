using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Services;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class AdlsResourceProvider(ITopazLogger logger) : ResourceProviderBase<AzureDataLakeStorageService>(logger)
{
    public void CreateFileSystem(StorageAccountResource storageAccount, string filesystemName, Filesystem filesystem)
    {
        base.Create(storageAccount.GetSubscription(), storageAccount.GetResourceGroup(), GetFilesystemId(storageAccount.Name, filesystemName), filesystem);
        
        var metadata = Path.Combine(GetFilesystemMetadataPath(storageAccount.GetSubscription(), storageAccount.GetResourceGroup(), storageAccount.Name, filesystemName));
        Directory.CreateDirectory(metadata);

        var data = GetFilesystemDataPath(storageAccount.GetSubscription(), storageAccount.GetResourceGroup(), storageAccount.Name, filesystemName);
        Directory.CreateDirectory(data);
    }
    
    private static string GetFilesystemId(string storageAccountName, string filesystemName)
    {
        return Path.Combine(storageAccountName, ".adls", filesystemName);
    }

    private string GetFilesystemMetadataPath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string filesystemName)
    {
        var blobPath =
            GetFilesystemPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, filesystemName);
        
        return Path.Combine(blobPath, ".metadata");
    }
    
    private string GetFilesystemPathWithReplacedValues(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string filesystemName)
    {
        var storageAccountPath =
            GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);

        filesystemName = PathGuard.SanitizeName(filesystemName);
        return Path.Combine(storageAccountPath, ".adls", filesystemName);
    }

    private string GetFilesystemDataPath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string filesystemName)
    {
        return Path.Combine(GetFilesystemPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, filesystemName), "data");
    }
}