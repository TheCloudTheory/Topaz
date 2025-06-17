using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Services;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class BlobResourceProvider(ITopazLogger logger) : ResourceProviderBase<BlobStorageService>(logger)
{
    private readonly ITopazLogger _topazLogger = logger;
    
    private void InitializeServiceDirectory(string storageAccountName)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, BlobStorageService.LocalDirectoryPath);
        this._topazLogger.LogDebug($"Attempting to create {servicePath} directory...");

        if(Directory.Exists(servicePath) == false)
        {
            Directory.CreateDirectory(servicePath);
            this._topazLogger.LogDebug($"Directory {servicePath} created.");
        }
        else
        {
            this._topazLogger.LogDebug($"Attempting to create {servicePath} directory - skipped.");
        }
    }
    
    public void Create(string containerName, string storageAccountName)
    {
        InitializeServiceDirectory(storageAccountName);
        
        var metadataFilePath = CreateBlobDirectories(storageAccountName, containerName);

        this._topazLogger.LogDebug($"Attempting to create {metadataFilePath} file.");

        if(File.Exists(metadataFilePath) == true) throw new InvalidOperationException($"Metadata file for {typeof(BlobStorageService)} with ID {containerName} already exists.");

        var content = JsonSerializer.Serialize(new Models.Container() { Name = containerName }, GlobalSettings.JsonOptions);
        File.WriteAllText(metadataFilePath, content);

        return;
    }

    private string CreateBlobDirectories(string storageAccountName, string containerName)
    {
        const string metadataFile = $"metadata.json";
        
        var containerPath = GetContainerPath(storageAccountName, containerName);
        var dataPath = Path.Combine(containerPath, "data");
        var metadataFilePath = Path.Combine(containerPath, metadataFile);
        var blobMetadataDirectoryPath = GetContainerMetadataPath(storageAccountName, containerName);

        this._topazLogger.LogDebug($"Attempting to create {containerPath} directory.");
        if(Directory.Exists(containerPath))
        {
            this._topazLogger.LogDebug($"Attempting to create {containerPath} directory - skipped.");
        }
        else
        {
            Directory.CreateDirectory(containerPath);
            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(blobMetadataDirectoryPath);
            
            this._topazLogger.LogDebug($"Attempting to create {containerPath} directory - created!");
        }

        return metadataFilePath;
    }
    
    public IEnumerable<string> List(string id)
    {
        InitializeServiceDirectory(id);

        var servicePath = Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, id, BlobStorageService.LocalDirectoryPath);
        return Directory.EnumerateDirectories(servicePath);
    }
    
    private string GetContainerPath(string storageAccountName, string containerName)
    {
        return Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, BlobStorageService.LocalDirectoryPath, containerName);
    }
    
    public string GetContainerDataPath(string storageAccountName, string containerName)
    {
        return Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, BlobStorageService.LocalDirectoryPath, containerName, "data");
    }
    
    public string GetContainerMetadataPath(string storageAccountName, string containerName)
    {
        return Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, BlobStorageService.LocalDirectoryPath, containerName, ".metadata");
    }
}