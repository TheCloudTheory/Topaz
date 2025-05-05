using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.Local.Service.Storage.Exceptions;
using Azure.Local.Service.Storage.Models;
using Azure.Local.Shared;
using Microsoft.AspNetCore.Http;

namespace Azure.Local.Service.Storage;

internal sealed class TableServiceDataPlane(TableServiceControlPlane controlPlane, ILogger logger)
{
    private readonly static JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly TableServiceControlPlane controlPlane = controlPlane;
    private readonly ILogger logger = logger;

    internal object InsertEntity(Stream input, string tableName, string storageAccountName)
    {
        this.logger.LogDebug($"Executing {nameof(InsertEntity)}: {tableName} {storageAccountName}");

        var path = this.controlPlane.GetTablePath(tableName, storageAccountName);

        using var sr = new StreamReader(input);

        var rawContent = sr.ReadToEnd();
        var content = JsonSerializer.Deserialize<GenericTableEntity>(rawContent, options) ?? throw new Exception();

        this.logger.LogDebug($"Executing {nameof(InsertEntity)}: Inserting {rawContent}.");

        var files = Directory.EnumerateFiles(path);
        var fileName = $"{content.PartitionKey}_{content.RowKey}.json";
        var entityPath = Path.Combine(path, fileName);

        if(File.Exists(entityPath))
        {
            // Duplicated entry
            this.logger.LogDebug($"Executing {nameof(InsertEntity)}: Duplicated entry.");
            throw new EntityAlreadyExistsException();
        }

        File.WriteAllText(entityPath, rawContent);

        return rawContent;
    }

    internal object?[] QueryEntities(QueryString query, string tableName, string storageAccountName)
    {
        this.logger.LogDebug($"Executing {nameof(QueryEntities)}: {query} {tableName} {storageAccountName}");

        // TODO: Add OData parser
        // string? filter = null;
        // var potentialFilter = query.Value.Split('&').FirstOrDefault(q => q.StartsWith("$filter"));
        // if(string.IsNullOrEmpty(potentialFilter) == false)
        // {
        //     filter = potentialFilter.Replace("$filter=", string.Empty);
        // }

        var path = this.controlPlane.GetTablePath(tableName, storageAccountName);
        var files = Directory.EnumerateFiles(path);
        var entities = files.Select(e => {
            var content = File.ReadAllText(e);
            return JsonSerializer.Deserialize<object>(content);
        }).ToArray();

        return entities; 
    }

    internal void UpdateEntity(Stream input, string tableName, string storageAccountName, string partitionKey, string rowKey)
    {
        this.logger.LogDebug($"Executing {nameof(InsertEntity)}: {tableName} {storageAccountName}");

        var path = this.controlPlane.GetTablePath(tableName, storageAccountName);

        using var sr = new StreamReader(input);

        var rawContent = sr.ReadToEnd();
        var content = JsonSerializer.Deserialize<GenericTableEntity>(rawContent, options) ?? throw new Exception();

        var fileName = $"{content.PartitionKey}_{content.RowKey}.json";
        var entityPath = Path.Combine(path, fileName);

        if(File.Exists(entityPath) == false)
        {
            // Not existing  entry
            this.logger.LogDebug($"Executing {nameof(InsertEntity)}: Not existing entry.");
            throw new EntityNotFoundException();
        }

        File.Delete(entityPath);

        File.WriteAllText(entityPath, rawContent);
    }
}
