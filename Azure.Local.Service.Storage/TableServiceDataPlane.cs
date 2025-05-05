using System.Text.Json;
using System.Text.RegularExpressions;
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

        var entities = File.ReadAllLines(path);
        foreach (var rawEntity in entities)
        {
            var entity = JsonSerializer.Deserialize<GenericTableEntity>(rawEntity, options);
            if (entity!.PartitionKey == content.PartitionKey && entity.RowKey == content.RowKey)
            {
                // Duplicated entry
                this.logger.LogDebug($"Executing {nameof(InsertEntity)}: Duplicated entry.");
                throw new EntityAlreadyExistsException();
            }
        }

        var oneLine = Regex.Replace(rawContent, @"\t|\n|\r", "");
        File.AppendAllLines(path, [oneLine]);

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
        var entities = File.ReadAllLines(path).Select(e => JsonSerializer.Deserialize<object>(e)).ToArray();

        return entities; 
    }
}
