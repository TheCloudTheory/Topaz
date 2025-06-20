using System.Text.Json;
using Azure.Data.Tables.Models;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Exceptions;
using Topaz.Service.Storage.Services;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class TableResourceProvider(ITopazLogger logger) : ResourceProviderBase<TableStorageService>(logger)
{
    private readonly ITopazLogger _topazLogger = logger;

    private void InitializeServiceDirectory(string storageAccountName)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, TableStorageService.LocalDirectoryPath);
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

    public IEnumerable<string> List(string id)
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
        
        var tablePath = this.GetTablePath(tableName, storageAccountName);

        if(Directory.Exists(tablePath) == false)
        {
            throw new EntityNotFoundException();
        }

        Directory.Delete(tablePath, true);
    }

    public void Create(string tableName, string storageAccountName, TableItem model)
    {
        InitializeServiceDirectory(storageAccountName);
        
        var metadataFilePath = CreateTableDirectories(tableName, storageAccountName);

        this._topazLogger.LogDebug($"Attempting to create {metadataFilePath} file.");

        if(File.Exists(metadataFilePath) == true) throw new InvalidOperationException($"Metadata file for {typeof(TableStorageService)} with ID {tableName} already exists.");

        var content = JsonSerializer.Serialize(model, GlobalSettings.JsonOptions);
        File.WriteAllText(metadataFilePath, content);

        return;
    }

    private string CreateTableDirectories(string tableName, string storageAccountName)
    {
        var metadataFile = $"metadata.json";
        var tablePath = this.GetTablePath(tableName, storageAccountName);
        var dataPath = Path.Combine(tablePath, "data");
        var aclPath = Path.Combine(tablePath, "acl");
        var metadataFilePath = Path.Combine(tablePath, metadataFile);

        this._topazLogger.LogDebug($"Attempting to create {tablePath} directory.");
        if(Directory.Exists(tablePath))
        {
            this._topazLogger.LogDebug($"Attempting to create {tablePath} directory - skipped.");
        }
        else
        {
            Directory.CreateDirectory(tablePath);
            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(aclPath);
            
            this._topazLogger.LogDebug($"Attempting to create {tablePath} directory - created!");
        }

        return metadataFilePath;
    }

    private string GetTablePath(string tableName, string storageAccountName)
    {
        return Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, TableStorageService.LocalDirectoryPath, tableName);
    }

    public string GetTableDataPath(string tableName, string storageAccountName)
    {
        return Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, TableStorageService.LocalDirectoryPath, tableName, "data");
    }

    public string GetTableAclPath(string tableName, string storageAccountName)
    {
        return Path.Combine(this.GetTablePath(tableName, storageAccountName), "acl");
    }

    public bool CheckIfTableExists(string tableName, string storageAccountName)
    {
        var tablePath = Path.Combine(BaseEmulatorPath, AzureStorageService.LocalDirectoryPath, storageAccountName, TableStorageService.LocalDirectoryPath, tableName);
        return Directory.Exists(tablePath);
    }
}
