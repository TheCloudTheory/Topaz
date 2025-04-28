using System.Text.Json;
using Azure.Data.Tables.Models;
using Azure.Local.Service.Storage.Models;
using Azure.Local.Shared;

namespace Azure.Local.Service.Storage;

internal sealed class TableServiceControlPlane
{
    private static readonly string TableServiceStoragePath = Path.Combine(AzureStorageService.LocalDirectoryPath, "table");

    public TableServiceControlPlane()
    {
        InitializeLocalDirectory();
    }

    private void InitializeLocalDirectory()
    {
        PrettyLogger.LogDebug("Attempting to create Table Service directory...");

        if (Directory.Exists(TableServiceStoragePath) == false)
        {
            Directory.CreateDirectory(TableServiceStoragePath);
            PrettyLogger.LogDebug("Local Table Service directory created.");
        }
        else
        {
            PrettyLogger.LogDebug("Attempting to create Table Service directory - skipped.");
        }
    }

    public TableItem[] GetTables(Stream input)
    {
        var tables = Directory.EnumerateFiles(TableServiceStoragePath);

        return [.. tables.Select(t => {
            var fileInfo = new FileInfo(t);
            var nameWithExtension = fileInfo.Name;
            var nameOnly = nameWithExtension.Split(".")[0];

            return new TableItem(nameOnly);
        })];
    }

    public TableItem CreateTable(Stream input)
    {
        using var sr = new StreamReader(input);

        var rawContent = sr.ReadToEnd();
        var content = JsonSerializer.Deserialize<TableProperties>(rawContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new Exception();

        var filePath = Path.Combine(AzureStorageService.LocalDirectoryPath, "table", content.TableName + ".jsonl");
        if(File.Exists(filePath))
        {
            throw new EntityAlreadyExistsException();
        }

        File.Create(filePath);

        return new TableItem(content.TableName);
    }
}

[Serializable]
internal class EntityAlreadyExistsException : Exception
{
    public EntityAlreadyExistsException()
    {
    }

    public EntityAlreadyExistsException(string? message) : base(message)
    {
    }

    public EntityAlreadyExistsException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}