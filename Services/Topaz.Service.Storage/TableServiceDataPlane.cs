using System.Text.Json;
using System.Text.Json.Nodes;
using Topaz.Service.Storage.Exceptions;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.OData;
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
        logger.LogDebug(nameof(TableServiceDataPlane), nameof(InsertEntity), "Executing {0}: {1} {2}", nameof(InsertEntity), tableName, storageAccountName);

        var path = resourceProvider.GetTableDataPath(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName);

        using var sr = new StreamReader(input);

        var rawContent = sr.ReadToEnd();
        var metadata = JsonSerializer.Deserialize<GenericTableEntity>(rawContent, GlobalSettings.JsonOptions) ?? throw new Exception();

        logger.LogDebug(nameof(TableServiceDataPlane), nameof(InsertEntity), "Executing {0}: Inserting {1}.", nameof(InsertEntity), rawContent);

        var safePartitionKey = PathGuard.SanitizeName(metadata.PartitionKey!);
        var safeRowKey = PathGuard.SanitizeName(metadata.RowKey!);

        var etag = new ETag(DateTimeOffset.Now.Ticks.ToString());
        var timestamp = DateTimeOffset.Now.ToUniversalTime();
        var fileName = $"{safePartitionKey}_{safeRowKey}.json";
        var entityPath = Path.Combine(path, fileName);
        PathGuard.EnsureWithinDirectory(entityPath, path);

        if(File.Exists(entityPath))
        {
            // Duplicated entry
            logger.LogDebug(nameof(TableServiceDataPlane), nameof(InsertEntity), "Executing {0}: Duplicated entry.", nameof(InsertEntity));
            throw new EntityAlreadyExistsException();
        } 

        var root = JsonNode.Parse(rawContent);
        root!["Timestamp"] = timestamp;
        root!["odata.etag"] = etag.ToString("H");

        var data = root.ToJsonString();

        File.WriteAllText(entityPath, data);

        return rawContent;
    }

    internal string GetEntity(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName,
        string partitionKey, string rowKey)
    {
        logger.LogDebug(nameof(TableServiceDataPlane), nameof(GetEntity), "Executing {0}: {1} {2}", nameof(GetEntity), tableName, storageAccountName);

        PathGuard.ValidateName(partitionKey);
        PathGuard.ValidateName(rowKey);

        var path = resourceProvider.GetTableDataPath(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName);

        var fileName = $"{PathGuard.SanitizeName(partitionKey)}_{PathGuard.SanitizeName(rowKey)}.json";
        var entityPath = Path.Combine(path, fileName);
        PathGuard.EnsureWithinDirectory(entityPath, path);

        if (!File.Exists(entityPath))
        {
            throw new EntityNotFoundException();
        }

        return File.ReadAllText(entityPath);
    }

    internal TableQueryResult QueryEntities(QueryString query, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName)
    {
        logger.LogDebug(nameof(TableServiceDataPlane), nameof(QueryEntities), "Executing {0}: {1} {2} {3}", nameof(QueryEntities), query, tableName, storageAccountName);

        var options = TableODataQueryOptions.Parse(query);

        var path = resourceProvider.GetTableDataPath(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName);

        // Load all entities from disk as JsonObject so we can inspect individual properties.
        var allEntities = Directory.EnumerateFiles(path)
            .Select(file =>
            {
                var content = File.ReadAllText(file);
                return JsonSerializer.Deserialize<JsonObject>(content, GlobalSettings.JsonOptions);
            })
            .Where(e => e is not null)
            .Select(e => e!)
            .ToList();

        // Sort by (PartitionKey ASC, RowKey ASC) for deterministic, stable paging.
        allEntities.Sort((a, b) =>
        {
            var pkA = a["PartitionKey"]?.GetValue<string>() ?? string.Empty;
            var pkB = b["PartitionKey"]?.GetValue<string>() ?? string.Empty;
            var pkCmp = string.Compare(pkA, pkB, StringComparison.Ordinal);
            if (pkCmp != 0) return pkCmp;
            var rkA = a["RowKey"]?.GetValue<string>() ?? string.Empty;
            var rkB = b["RowKey"]?.GetValue<string>() ?? string.Empty;
            return string.Compare(rkA, rkB, StringComparison.Ordinal);
        });

        // Apply continuation seek: skip entities that precede the (NextPartitionKey, NextRowKey) cursor.
        IEnumerable<JsonObject> sequence = allEntities;
        if (options.NextPartitionKey is not null)
        {
            var nextPk = options.NextPartitionKey;
            var nextRk = options.NextRowKey ?? string.Empty;
            sequence = sequence.SkipWhile(e =>
            {
                var pk = e["PartitionKey"]?.GetValue<string>() ?? string.Empty;
                var rk = e["RowKey"]?.GetValue<string>() ?? string.Empty;
                var pkCmp = string.Compare(pk, nextPk, StringComparison.Ordinal);
                return pkCmp < 0 || (pkCmp == 0 && string.Compare(rk, nextRk, StringComparison.Ordinal) < 0);
            });
        }

        // Apply $filter.
        if (options.Filter is not null)
            sequence = sequence.Where(e => TableODataFilter.Evaluate(options.Filter, e));

        // Materialise so we can index for paging.
        var filtered = sequence.ToList();

        // Determine page boundary and populate continuation keys when more entities follow.
        string? contNextPk = null;
        string? contNextRk = null;
        List<JsonObject> page;

        if (options.Top.HasValue && filtered.Count > options.Top.Value)
        {
            page = filtered.Take(options.Top.Value).ToList();
            var firstExcluded = filtered[options.Top.Value];
            contNextPk = firstExcluded["PartitionKey"]?.GetValue<string>();
            contNextRk = firstExcluded["RowKey"]?.GetValue<string>();
        }
        else
        {
            page = filtered;
        }

        // Apply $select projection: create a new JsonObject containing only the requested properties.
        object?[] result;
        if (options.Select is not null)
        {
            result = page
                .Select(e =>
                {
                    var projected = new JsonObject();
                    foreach (var prop in options.Select)
                    {
                        if (e.TryGetPropertyValue(prop, out var value))
                            projected[prop] = value?.DeepClone();
                    }
                    return (object?)projected;
                })
                .ToArray();
        }
        else
        {
            result = page.Cast<object?>().ToArray();
        }

        return new TableQueryResult(result, contNextPk, contNextRk);
    }

    internal void DeleteEntity(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName,
        string partitionKey, string rowKey, IHeaderDictionary headers)
    {
        var etag = headers["If-Match"].FirstOrDefault() ?? "*";
        DeleteEntity(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName,
            partitionKey, rowKey, etag);
    }

    internal void DeleteEntity(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName,
        string partitionKey, string rowKey, string etag = "*")
    {
        logger.LogDebug(nameof(TableServiceDataPlane), nameof(DeleteEntity), "Executing {0}: {1} {2}", nameof(DeleteEntity), tableName, storageAccountName);

        PathGuard.ValidateName(partitionKey);
        PathGuard.ValidateName(rowKey);

        var path = resourceProvider.GetTableDataPath(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName);

        var fileName = $"{PathGuard.SanitizeName(partitionKey)}_{PathGuard.SanitizeName(rowKey)}.json";
        var entityPath = Path.Combine(path, fileName);
        PathGuard.EnsureWithinDirectory(entityPath, path);

        if (!File.Exists(entityPath))
        {
            logger.LogDebug(nameof(TableServiceDataPlane), nameof(DeleteEntity), "Executing {0}: Entity not found.", nameof(DeleteEntity));
            throw new EntityNotFoundException();
        }

        if (etag != "*")
        {
            var file = File.ReadAllText(entityPath);
            var currentData = JsonSerializer.Deserialize<GenericTableEntity>(file, GlobalSettings.JsonOptions) ??
                throw new Exception("Cannot proceed if entity data is null.");
            if (currentData.ETag.ToString() != etag) throw new UpdateConditionNotSatisfiedException();
        }

        File.Delete(entityPath);
    }

    internal void UpsertEntity(Stream input, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName,
        string partitionKey, string rowKey)
    {
        logger.LogDebug(nameof(TableServiceDataPlane), nameof(UpsertEntity), "Executing {0}: {1} {2}", nameof(UpsertEntity), tableName, storageAccountName);

        PathGuard.ValidateName(partitionKey);
        PathGuard.ValidateName(rowKey);

        var path = resourceProvider.GetTableDataPath(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName);

        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();

        var fileName = $"{PathGuard.SanitizeName(partitionKey)}_{PathGuard.SanitizeName(rowKey)}.json";
        var entityPath = Path.Combine(path, fileName);
        PathGuard.EnsureWithinDirectory(entityPath, path);

        var root = JsonNode.Parse(rawContent) ?? throw new Exception("Cannot parse entity body.");
        root["PartitionKey"] = partitionKey;
        root["RowKey"] = rowKey;
        root["Timestamp"] = DateTimeOffset.Now.ToUniversalTime();
        root["odata.etag"] = DateTimeOffset.Now.Ticks;

        File.WriteAllText(entityPath, root.ToJsonString());
    }

    internal void UpdateEntity(Stream input, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName, string partitionKey,
                               string rowKey, IHeaderDictionary headers)
    {
        logger.LogDebug(nameof(TableServiceDataPlane), nameof(UpdateEntity), "Executing {0}: {1} {2}", nameof(UpdateEntity), tableName, storageAccountName);

        PathGuard.ValidateName(partitionKey);
        PathGuard.ValidateName(rowKey);

        var etag = headers["If-Match"];
        var path = resourceProvider.GetTableDataPath(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName);

        using var sr = new StreamReader(input, leaveOpen: true);

        var rawContent = sr.ReadToEnd();

        var fileName = $"{PathGuard.SanitizeName(partitionKey)}_{PathGuard.SanitizeName(rowKey)}.json";
        var entityPath = Path.Combine(path, fileName);
        PathGuard.EnsureWithinDirectory(entityPath, path);

        if(File.Exists(entityPath) == false)
        {
            // Not existing entry — for upsert callers, the stream is left open so they can retry via UpsertEntity.
            logger.LogDebug(nameof(TableServiceDataPlane), nameof(UpdateEntity), "Executing {0}: Not existing entry.", nameof(UpdateEntity));
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
