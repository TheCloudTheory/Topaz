using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Services;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class BlobResourceProvider(ILogger logger) : ResourceProviderBase<BlobStorageService>(logger)
{
    private readonly ILogger logger = logger;

    protected override void InitializeServiceDirectory()
    {
        // Just discard the base implementation as Blob Storage is a service inside a service
        // and requires slightly different initialization
    }
    
    private void InitializeServiceDirectory(string storageAccountName)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, BlobStorageService.LocalDirectoryPath);
        this.logger.LogDebug($"Attempting to create {servicePath} directory...");

        if(Directory.Exists(servicePath) == false)
        {
            Directory.CreateDirectory(servicePath);
            this.logger.LogDebug($"Directory {servicePath} created.");
        }
        else
        {
            this.logger.LogDebug($"Attempting to create {servicePath} directory - skipped.");
        }
    }
    
    public void Create(string containerName, string storageAccountName)
    {
        InitializeServiceDirectory(storageAccountName);
        
        var metadataFilePath = CreateBlobDirectories(containerName, storageAccountName);

        this.logger.LogDebug($"Attempting to create {metadataFilePath} file.");

        if(File.Exists(metadataFilePath) == true) throw new InvalidOperationException($"Metadata file for {typeof(BlobStorageService)} with ID {containerName} already exists.");

        var content = JsonSerializer.Serialize(new Models.Container() { Name = containerName }, GlobalSettings.JsonOptions);
        File.WriteAllText(metadataFilePath, content);

        return;
    }

    private string CreateBlobDirectories(string containerName, string storageAccountName)
    {
        var metadataFile = $"metadata.json";
        var containerPath = this.GetContainerPath(storageAccountName, containerName);
        var dataPath = Path.Combine(containerPath, "data");
        var metadataFilePath = Path.Combine(containerPath, metadataFile);

        this.logger.LogDebug($"Attempting to create {containerPath} directory.");
        if(Directory.Exists(containerPath))
        {
            this.logger.LogDebug($"Attempting to create {containerPath} directory - skipped.");
        }
        else
        {
            Directory.CreateDirectory(containerPath);
            Directory.CreateDirectory(dataPath);
            
            this.logger.LogDebug($"Attempting to create {containerPath} directory - created!");
        }

        return metadataFilePath;
    }
    
    public override IEnumerable<string> List(string id)
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
}