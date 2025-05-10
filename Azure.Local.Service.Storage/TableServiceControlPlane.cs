using System.Text.Json;
using Azure.Data.Tables.Models;
using Azure.Local.Service.Storage.Exceptions;
using Azure.Local.Service.Storage.Models;

namespace Azure.Local.Service.Storage;

internal sealed class TableServiceControlPlane(TableResourceProvider provider)
{
    private readonly TableResourceProvider provider = provider;

    public TableProperties[] GetTables(string storageAccountName)
    {
        var tables = this.provider.List(storageAccountName);

        return [.. tables.Select(t => {
            var di = new DirectoryInfo(t);
            return new TableProperties(di.Name);
        })];
    }

    public TableItem CreateTable(Stream input, string storageAccountName)
    {
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
