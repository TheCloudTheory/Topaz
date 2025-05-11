using System.Text.Json;
using Azure.Data.Tables.Models;
using Azure.Local.Service.Shared;
using Azure.Local.Service.Storage.Exceptions;
using Azure.Local.Shared;

namespace Azure.Local.Service.Storage;

internal sealed class TableResourceProvider(ILogger logger) : ResourceProviderBase<TableStorageService>(logger)
{
    private readonly ILogger logger = logger;

    protected override void InitializeServiceDirectory()
    {
        // Just discard the base implementation as Table Storage is a service inside a service
        // and requires slightly different initialization
    }

    private void InitializeServiceDirectory(string storageAccountName)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, TableStorageService.LocalDirectoryPath);
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

    public override IEnumerable<string> List(string id)
    {
        InitializeServiceDirectory(id);

        var servicePath = Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, id, TableStorageService.LocalDirectoryPath);
        return Directory.EnumerateDirectories(servicePath);
    }

    public override void Delete(string id)
    {
    }

    public void Delete(string tableName, string storageAccountName)
    {
        InitializeServiceDirectory(storageAccountName);

        var tablePath = Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, TableStorageService.LocalDirectoryPath, tableName);

        if(Directory.Exists(tablePath) == false)
        {
            throw new EntityNotFoundException();
        }

        Directory.Delete(tablePath, true);
    }

    public override void Create<TModel>(string id, TModel model)
    {
    }

    public void Create(string tableName, string storageAccountName, TableItem model)
    {
        var fileName = $"metadata.json";
        var tablePath = Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, TableStorageService.LocalDirectoryPath, tableName);
        var dataPath = Path.Combine(tablePath, "data");
        var metadataFilePath = Path.Combine(tablePath, fileName);

        this.logger.LogDebug($"Attempting to create {tablePath} directory.");
        if(Directory.Exists(tablePath))
        {
            this.logger.LogDebug($"Attempting to create {tablePath} directory - skipped.");
        }
        else
        {
            Directory.CreateDirectory(tablePath);
            Directory.CreateDirectory(dataPath);
            this.logger.LogDebug($"Attempting to create {tablePath} directory - created!");
        }

        this.logger.LogDebug($"Attempting to create {metadataFilePath} file.");

        if(File.Exists(metadataFilePath) == true) throw new InvalidOperationException($"Metadata file for {typeof(TableStorageService)} with ID {tableName} already exists.");

        var content = JsonSerializer.Serialize(model, GlobalSettings.JsonOptions);
        File.WriteAllText(metadataFilePath, content);

        return;
    }

    public string GetTablePath(string tableName, string storageAccountName)
    {
        return Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, TableStorageService.LocalDirectoryPath, tableName, "data");
    }

    public bool CheckIfTableExists(string tableName, string storageAccountName)
    {
        var tablePath = Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, TableStorageService.LocalDirectoryPath, tableName);
        return Directory.Exists(tablePath);
    }
}
