using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Services;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class BlobResourceProvider(ITopazLogger logger) : ResourceProviderBase<BlobStorageService>(logger)
{
    public void Create(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName, Container container)
    {
        base.Create(subscriptionIdentifier, resourceGroupIdentifier, GetContainerId(storageAccountName, containerName), container);
        
        var metadata = Path.Combine(GetContainerMetadataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName));
        Directory.CreateDirectory(metadata);
    }
    
    private static string GetContainerId(string storageAccountName, string containerName)
    {
        return Path.Combine(storageAccountName, ".blob", containerName);
    }
    
    public string GetContainerDataPath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName)
    {
        return Path.Combine(GetContainerPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName), "data");
    }
    
    public string GetContainerMetadataPath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName)
    {
        var tablePath =
            GetContainerPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
        
        return Path.Combine(tablePath, ".metadata");
    }
    
    private string GetContainerPathWithReplacedValues(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName)
    {
        var storageAccountPath =
            GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);

        return Path.Combine(storageAccountPath, containerName);
    }
}