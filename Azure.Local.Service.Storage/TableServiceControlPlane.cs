using System.Text.Json;
using Azure.Data.Tables.Models;
using Azure.Local.Service.Storage.Exceptions;
using Azure.Local.Service.Storage.Models;
using Azure.Local.Shared;

namespace Azure.Local.Service.Storage;

internal sealed class TableServiceControlPlane(ILogger logger)
{
    private readonly ILogger logger = logger;

    private void InitializeLocalDirectory(string storageAccountName)
    {
        this.logger.LogDebug("Attempting to create Table Service directory...");
        var path = Path.Combine(AzureStorageService.LocalDirectoryPath, storageAccountName);

        if (Directory.Exists(path) == false)
        {
            Directory.CreateDirectory(path);
            this.logger.LogDebug("Local Table Service directory created.");
        }
        else
        {
            this.logger.LogDebug("Attempting to create Table Service directory - skipped.");
        }
    }

    public TableProperties[] GetTables(Stream input, string storageAccountName)
    {
        var path = Path.Combine(AzureStorageService.LocalDirectoryPath, storageAccountName);
        var tables = Directory.EnumerateDirectories(path);

        return [.. tables.Select(t => {
            var di = new DirectoryInfo(t);
            return new TableProperties(di.Name);
        })];
    }

    public TableItem CreateTable(Stream input, string storageAccountName)
    {
        InitializeLocalDirectory(storageAccountName);

        using var sr = new StreamReader(input);

        var rawContent = sr.ReadToEnd();
        var content = JsonSerializer.Deserialize<TableProperties>(rawContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new Exception();

        var directoryPath = Path.Combine(AzureStorageService.LocalDirectoryPath, storageAccountName, content.TableName);
        if(Directory.Exists(directoryPath))
        {
            throw new EntityAlreadyExistsException();
        }

        Directory.CreateDirectory(directoryPath);

        return new TableItem(content.TableName);
    }

    public void DeleteTable(string tableName, string storageAccountName)
    {
        var directoryPath = Path.Combine(AzureStorageService.LocalDirectoryPath, storageAccountName, tableName);
        if(Directory.Exists(directoryPath) == false)
        {
            throw new EntityNotFoundException();
        }

        Directory.Delete(directoryPath);
    }

    internal bool CheckIfTableExists(string tableName, string storageAccountName)
    {
        var path = Path.Combine(AzureStorageService.LocalDirectoryPath, storageAccountName, tableName);

        return Directory.Exists(path);
    }

    internal string GetTablePath(string tableName, string storageAccountName)
    {
        return Path.Combine(AzureStorageService.LocalDirectoryPath, storageAccountName, tableName);
    }
}
