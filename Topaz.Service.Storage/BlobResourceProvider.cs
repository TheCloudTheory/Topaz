using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Services;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class BlobResourceProvider(ITopazLogger logger) : ResourceProviderBase<BlobStorageService>(logger)
{
    private readonly ITopazLogger _logger = logger;
    
    private void InitializeServiceDirectory(string storageAccountName)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, BlobStorageService.LocalDirectoryPath);
        _logger.LogDebug($"Attempting to create {servicePath} directory...");

        if(!Directory.Exists(servicePath))
        {
            Directory.CreateDirectory(servicePath);
            _logger.LogDebug($"Directory {servicePath} created.");
        }
        else
        {
            _logger.LogDebug($"Attempting to create {servicePath} directory - skipped.");
        }
    }
    
    public void Create(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName, Container container)
    {
        base.Create(subscriptionIdentifier, resourceGroupIdentifier, GetContainerId(storageAccountName, containerName), container);
    }
    
    private static string GetContainerId(string storageAccountName, string containerName)
    {
        return Path.Combine(storageAccountName, ".blob", containerName);
    }

    private string CreateBlobDirectories(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName)
    {
        const string metadataFile = "metadata.json";
        
        var containerPath = GetContainerPath(storageAccountName, containerName);
        var dataPath = Path.Combine(containerPath, "data");
        var metadataFilePath = Path.Combine(containerPath, metadataFile);
        var blobMetadataDirectoryPath = GetContainerMetadataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);

        _logger.LogDebug($"Attempting to create {containerPath} directory.");
        if(Directory.Exists(containerPath))
        {
            _logger.LogDebug($"Attempting to create {containerPath} directory - skipped.");
        }
        else
        {
            Directory.CreateDirectory(containerPath);
            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(blobMetadataDirectoryPath);
            
            _logger.LogDebug($"Attempting to create {containerPath} directory - created!");
        }

        return metadataFilePath;
    }
    
    public IEnumerable<string> List(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, GetLocalDirectoryPathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier), storageAccountName);
        if (!Directory.Exists(servicePath))
        {
            _logger.LogWarning("Trying to list resources for a non-existing storage account. If you see this warning, make sure you created a storage account before accessing its data.");
            return [];
        }
        
        var metadataFiles = Directory.EnumerateFiles(servicePath, "metadata.json", SearchOption.AllDirectories);
        return metadataFiles.Select(File.ReadAllText);
    }
    
    private string GetContainerPath(string storageAccountName, string containerName)
    {
        return Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, BlobStorageService.LocalDirectoryPath, containerName);
    }
    
    public string GetContainerDataPath(string storageAccountName, string containerName)
    {
        return Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, BlobStorageService.LocalDirectoryPath, containerName, "data");
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