using System.Text.Json;
using Azure.Data.Tables.Models;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;

namespace Topaz.Service.Storage;

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
        var content = JsonSerializer.Deserialize<TableProperties>(rawContent, GlobalSettings.JsonOptions) 
            ?? throw new Exception();
        var model = new TableItem(content.TableName);;

        this.provider.Create(content.TableName, storageAccountName, model);

        return model;
    }

    public void DeleteTable(string tableName, string storageAccountName)
    {
       this.provider.Delete(tableName, storageAccountName);
    }

    internal bool CheckIfTableExists(string tableName, string storageAccountName)
    {
        return this.provider.CheckIfTableExists(tableName, storageAccountName);
    }

    internal string GetTablePath(string tableName, string storageAccountName)
    {
        return this.provider.GetTablePath(tableName, storageAccountName);
    }
}
