using System.Text.Json;
using System.Text.Json.Nodes;
using Topaz.Service.Storage.Exceptions;
using Topaz.Service.Storage.Models;
using Topaz.Shared;
using Microsoft.AspNetCore.Http;
using Azure;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.Storage;

internal sealed class TableServiceDataPlane(TableResourceProvider resourceProvider, ITopazLogger logger)
{
    internal string InsertEntity(Stream input, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName)
    {
        logger.LogDebug($"Executing {nameof(InsertEntity)}: {tableName} {storageAccountName}");

        var path = resourceProvider.GetTableDataPath(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName);

        using var sr = new StreamReader(input);

        var rawContent = sr.ReadToEnd();
        var metadata = JsonSerializer.Deserialize<GenericTableEntity>(rawContent, GlobalSettings.JsonOptions) ?? throw new Exception();

        logger.LogDebug($"Executing {nameof(InsertEntity)}: Inserting {rawContent}.");

        var etag = new ETag(DateTimeOffset.Now.Ticks.ToString());
        var timestamp = DateTimeOffset.Now.ToUniversalTime();
        var fileName = $"{metadata.PartitionKey}_{metadata.RowKey}.json";
        var entityPath = Path.Combine(path, fileName);

        if(File.Exists(entityPath))
        {
            // Duplicated entry
            logger.LogDebug($"Executing {nameof(InsertEntity)}: Duplicated entry.");
            throw new EntityAlreadyExistsException();
        } 

        var root = JsonNode.Parse(rawContent);
        root!["Timestamp"] = timestamp;
        root!["odata.etag"] = etag.ToString("H");

        var data = root.ToJsonString();

        File.WriteAllText(entityPath, data);

        return rawContent;
    }

    internal object?[] QueryEntities(QueryString query, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName)
    {
        logger.LogDebug($"Executing {nameof(QueryEntities)}: {query} {tableName} {storageAccountName}");

        // TODO: Add OData parser
        // string? filter = null;
        // var potentialFilter = query.Value.Split('&').FirstOrDefault(q => q.StartsWith("$filter"));
        // if(string.IsNullOrEmpty(potentialFilter) == false)
        // {
        //     filter = potentialFilter.Replace("$filter=", string.Empty);
        // }

        var path = resourceProvider.GetTableDataPath(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName);
        var files = Directory.EnumerateFiles(path);
        var entities = files.Select(e => {
            var content = File.ReadAllText(e);
            return JsonSerializer.Deserialize<object>(content, GlobalSettings.JsonOptions);
        }).ToArray();

        return entities; 
    }

    internal void UpdateEntity(Stream input, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName, string partitionKey,
                               string rowKey, IHeaderDictionary headers)
    {
        logger.LogDebug($"Executing {nameof(InsertEntity)}: {tableName} {storageAccountName}");

        var etag = headers["If-Match"];
        var path = resourceProvider.GetTableDataPath(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName);

        using var sr = new StreamReader(input);

        var rawContent = sr.ReadToEnd();

        var fileName = $"{partitionKey}_{rowKey}.json";
        var entityPath = Path.Combine(path, fileName);

        if(File.Exists(entityPath) == false)
        {
            // Not existing  entry
            logger.LogDebug($"Executing {nameof(InsertEntity)}: Not existing entry.");
            throw new EntityNotFoundException();
        }

        if(etag != "*")
        {
            var file = File.ReadAllText(entityPath);
            var currentData = JsonSerializer.Deserialize<GenericTableEntity>(file, GlobalSettings.JsonOptions) ?? 
                throw new Exception("Cannot proceed if entity data is null.");
            if (currentData.ETag.ToString() != etag) throw new UpdateConditionNotSatisfiedException();
        }

        File.Delete(entityPath);

        var root = JsonNode.Parse(rawContent);
        var newEtag = DateTimeOffset.Now.Ticks;
        var timestamp = DateTimeOffset.Now.ToUniversalTime();

        root!["Timestamp"] = timestamp;
        root!["odata.etag"] = newEtag;

        var data = root.ToJsonString();

        File.WriteAllText(entityPath, data);
    }
}
