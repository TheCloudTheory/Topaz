using System.Text.Json;
using Azure.Data.Tables.Models;
using Azure.Local.Service.Storage.Models;
using Azure.Local.Shared;

namespace Azure.Local.Service.Storage;

internal sealed class TableServiceControlPlane
{
    private static readonly string TableServiceStoragePath = Path.Combine(AzureStorageService.LocalDirectoryPath, "table");
    private readonly ILogger logger;

    public TableServiceControlPlane(ILogger logger)
    {
        this.logger = logger;

        InitializeLocalDirectory();
    }

    private void InitializeLocalDirectory()
    {
        this.logger.LogDebug("Attempting to create Table Service directory...");

        if (Directory.Exists(TableServiceStoragePath) == false)
        {
            Directory.CreateDirectory(TableServiceStoragePath);
            this.logger.LogDebug("Local Table Service directory created.");
        }
        else
        {
            this.logger.LogDebug("Attempting to create Table Service directory - skipped.");
        }
    }

    public TableProperties[] GetTables(Stream input)
    {
        var tables = Directory.EnumerateFiles(TableServiceStoragePath);

        return [.. tables.Select(t => {
            var fileInfo = new FileInfo(t);
            var nameWithExtension = fileInfo.Name;
            var nameOnly = nameWithExtension.Split(".")[0];

            return new TableProperties(nameOnly);
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

    public void DeleteTable(string input)
    {
        var filePath = Path.Combine(AzureStorageService.LocalDirectoryPath, "table", input + ".jsonl");
        if(File.Exists(filePath) == false)
        {
            throw new EntityNotFoundException();
        }

        File.Delete(filePath);
    }
}

[Serializable]
internal class EntityNotFoundException : Exception
{
    public EntityNotFoundException()
    {
    }

    public EntityNotFoundException(string? message) : base(message)
    {
    }

    public EntityNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
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