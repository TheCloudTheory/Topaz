using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.CosmosDb.Models;
using Topaz.Service.CosmosDb.Models.Requests;
using Topaz.Service.CosmosDb.SqlQuery;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb;

/// <summary>
/// Resolves an incoming Cosmos DB data-plane HTTP request to the corresponding
/// <see cref="DatabaseAccountResource"/> persisted by the control plane.
///
/// The account name is extracted from the first DNS label of the request's Host header
/// (e.g. <c>myaccount</c> from <c>myaccount.documents.topaz.local.dev:8895</c>) and
/// looked up in the global DNS registry.
/// </summary>
internal sealed class CosmosDbDataPlane(DatabaseAccountResourceProvider provider, ITopazLogger logger)
{
    private const string SqlDatabasesSubresource = "sqldatabases";
    private const string SqlContainersSubresource = "sqlcontainers";

    private static string SqlContainerParentId(string accountName, string databaseName) =>
        $"{accountName}/{SqlDatabasesSubresource}/{databaseName}";

    /// <summary>
    /// Resolves the Cosmos DB account associated with the incoming request.
    /// Returns <c>null</c> when the host header cannot be mapped to a known account.
    /// </summary>
    internal DatabaseAccountResource? ResolveAccount(HttpContext context)
    {
        var accountName = context.Request.Host.Host.Split('.')[0];

        var identifiers = GlobalDnsEntries.GetEntry(CosmosDbService.UniqueName, accountName);
        if (identifiers == null)
        {
            logger.LogDebug(nameof(CosmosDbDataPlane), nameof(ResolveAccount),
                "No DNS entry found for Cosmos DB account '{0}'", accountName);
            return null;
        }

        var sub = SubscriptionIdentifier.From(identifiers.Value.subscription);
        var rg = ResourceGroupIdentifier.From(identifiers.Value.resourceGroup!);

        var resource = provider.GetAs<DatabaseAccountResource>(sub, rg, accountName);
        if (resource == null)
        {
            logger.LogDebug(nameof(CosmosDbDataPlane), nameof(ResolveAccount),
                "Cosmos DB account '{0}' found in DNS but not persisted on disk", accountName);
        }

        return resource;
    }

    private (DatabaseAccountResource Account, SubscriptionIdentifier Sub, ResourceGroupIdentifier Rg)? ResolveAccountContext(HttpContext context)
    {
        var accountName = context.Request.Host.Host.Split('.')[0];
        var identifiers = GlobalDnsEntries.GetEntry(CosmosDbService.UniqueName, accountName);
        if (identifiers == null) return null;

        var sub = SubscriptionIdentifier.From(identifiers.Value.subscription);
        var rg = ResourceGroupIdentifier.From(identifiers.Value.resourceGroup!);
        var account = provider.GetAs<DatabaseAccountResource>(sub, rg, accountName);
        if (account == null) return null;

        return (account, sub, rg);
    }

    /// <summary>Creates or updates a SQL database and returns its inner resource representation.</summary>
    internal DataPlaneOperationResult<SqlDatabaseInnerResource> CreateDatabase(HttpContext context, string databaseName, int? throughput)
    {
        var ctx = ResolveAccountContext(context);
        if (ctx == null) return new DataPlaneOperationResult<SqlDatabaseInnerResource>(OperationResult.NotFound, null, "Account not found.", "AccountNotFound");
        var (account, sub, rg) = ctx.Value;

        var request = new CreateOrUpdateSqlDatabaseRequest
        {
            Properties = new CreateOrUpdateSqlDatabaseRequest.CreateOrUpdateSqlDatabaseRequestProperties
            {
                Resource = new CreateOrUpdateSqlDatabaseRequest.CreateOrUpdateSqlDatabaseResourceInfo { Id = databaseName },
                Options = throughput.HasValue ? new SqlDatabaseOptions { Throughput = throughput } : null
            }
        };

        var existing = provider.GetSubresourceAs<SqlDatabaseResource>(sub, rg, databaseName, account.Name, SqlDatabasesSubresource);
        if (existing != null)
        {
            SqlDatabaseResourceProperties.UpdateFromRequest(existing.Properties, request);
            provider.CreateOrUpdateSubresource(sub, rg, databaseName, account.Name, SqlDatabasesSubresource, existing);
            return new DataPlaneOperationResult<SqlDatabaseInnerResource>(OperationResult.Updated, existing.Properties.Resource, null, null);
        }

        var props = SqlDatabaseResourceProperties.FromRequest(databaseName, request);
        var resource = new SqlDatabaseResource(sub, rg, account.Name, databaseName, props);
        provider.CreateOrUpdateSubresource(sub, rg, databaseName, account.Name, SqlDatabasesSubresource, resource);
        return new DataPlaneOperationResult<SqlDatabaseInnerResource>(OperationResult.Created, props.Resource, null, null);
    }

    /// <summary>Returns the inner resource for a named SQL database, or NotFound if not found.</summary>
    internal DataPlaneOperationResult<SqlDatabaseInnerResource> GetDatabase(HttpContext context, string databaseName)
    {
        var ctx = ResolveAccountContext(context);
        if (ctx == null) return new DataPlaneOperationResult<SqlDatabaseInnerResource>(OperationResult.NotFound, null, "Account not found.", "AccountNotFound");
        var (account, sub, rg) = ctx.Value;

        var database = provider.GetSubresourceAs<SqlDatabaseResource>(sub, rg, databaseName, account.Name, SqlDatabasesSubresource);
        return database == null
            ? new DataPlaneOperationResult<SqlDatabaseInnerResource>(OperationResult.NotFound, null, $"Database '{databaseName}' not found.", "DatabaseNotFound")
            : new DataPlaneOperationResult<SqlDatabaseInnerResource>(OperationResult.Success, database.Properties.Resource, null, null);
    }

    /// <summary>Deletes a named SQL database.</summary>
    internal DataPlaneOperationResult DeleteDatabase(HttpContext context, string databaseName)
    {
        var ctx = ResolveAccountContext(context);
        if (ctx == null) return new DataPlaneOperationResult(OperationResult.NotFound, "Account not found.", "AccountNotFound");
        var (account, sub, rg) = ctx.Value;

        var database = provider.GetSubresourceAs<SqlDatabaseResource>(sub, rg, databaseName, account.Name, SqlDatabasesSubresource);
        if (database == null) return new DataPlaneOperationResult(OperationResult.NotFound, $"Database '{databaseName}' not found.", "DatabaseNotFound");

        provider.DeleteSubresource(sub, rg, databaseName, account.Name, SqlDatabasesSubresource);
        return new DataPlaneOperationResult(OperationResult.Deleted, null, null);
    }

    /// <summary>Lists all SQL databases for the account resolved from the request host.</summary>
    internal DataPlaneOperationResult<SqlDatabaseInnerResource[]> ListDatabases(HttpContext context)
    {
        var ctx = ResolveAccountContext(context);
        if (ctx == null) return new DataPlaneOperationResult<SqlDatabaseInnerResource[]>(OperationResult.NotFound, null, "Account not found.", "AccountNotFound");
        var (account, sub, rg) = ctx.Value;

        var databases = provider.ListSubresourcesAs<SqlDatabaseResource>(sub, rg, account.Name, SqlDatabasesSubresource);
        return new DataPlaneOperationResult<SqlDatabaseInnerResource[]>(OperationResult.Success, databases.Select(d => d.Properties.Resource).ToArray(), null, null);
    }

    /// <summary>
    /// Returns a minimal query execution plan for cross-partition and ORDER BY queries.
    ///
    /// The Cosmos SDK sends a query-plan request (<c>x-ms-cosmos-is-query-plan-request: True</c>)
    /// before executing any query that requires cross-partition fan-out. Topaz models a
    /// single-partition container, so the plan always covers the full hash range with empty
    /// <c>orderBy</c> / <c>aggregates</c> fields, causing the SDK to use simple parallel
    /// execution and send the original SQL unchanged.
    /// </summary>
    internal static JsonObject GetQueryPlan(string query) => new()
    {
        ["partitionedQueryExecutionInfoVersion"] = 2,
        ["queryInfo"] = new JsonObject
        {
            ["distinctType"] = "None",
            ["top"] = JsonValue.Create((int?)null),
            ["offset"] = JsonValue.Create((int?)null),
            ["limit"] = JsonValue.Create((int?)null),
            ["orderBy"] = new JsonArray(),
            ["orderByExpressions"] = new JsonArray(),
            ["aggregates"] = new JsonArray(),
            ["groupByExpressions"] = new JsonArray(),
            ["groupByAliases"] = new JsonArray(),
            ["rewrittenQuery"] = query,
            ["hasSelectValue"] = false,
            ["hasNonStreamingOrderBy"] = true
        },
        ["queryRanges"] = new JsonArray
        {
            new JsonObject
            {
                ["min"] = "",
                ["max"] = "FF",
                ["isMinInclusive"] = true,
                ["isMaxInclusive"] = false
            }
        }
    };

    /// <summary>Returns the account properties response for the account resolved from the request host.</summary>
    internal DataPlaneOperationResult<AccountPropertiesResponse> GetAccountProperties(HttpContext context)
    {
        var account = ResolveAccount(context);
        if (account?.Properties == null) return new DataPlaneOperationResult<AccountPropertiesResponse>(OperationResult.NotFound, null, "Account not found.", "AccountNotFound");

        var props = account.Properties;
        var body = new AccountPropertiesResponse
        {
            Id = account.Name,
            WriteLocations = props.WriteLocations,
            ReadLocations = props.ReadLocations,
            ConsistencyPolicy = props.ConsistencyPolicy ?? new ConsistencyPolicySettings(),
            DocumentEndpoint = props.DocumentEndpoint ?? string.Empty
        };
        return new DataPlaneOperationResult<AccountPropertiesResponse>(OperationResult.Success, body, null, null);
    }

    /// <summary>Creates a new collection and returns its inner resource representation. Returns <c>Updated</c> (→ 409) when the collection already exists.</summary>
    internal DataPlaneOperationResult<SqlContainerInnerResource> CreateCollection(
        HttpContext context, string databaseName, string collectionName,
        int? throughput, ContainerPartitionKey? partitionKey, object? indexingPolicy, object? uniqueKeyPolicy, int? defaultTtl)
    {
        var ctx = ResolveAccountContext(context);
        if (ctx == null) return new DataPlaneOperationResult<SqlContainerInnerResource>(OperationResult.NotFound, null, "Account not found.", "AccountNotFound");
        var (account, sub, rg) = ctx.Value;

        var database = provider.GetSubresourceAs<SqlDatabaseResource>(sub, rg, databaseName, account.Name, SqlDatabasesSubresource);
        if (database == null) return new DataPlaneOperationResult<SqlContainerInnerResource>(OperationResult.NotFound, null, $"Database '{databaseName}' not found.", "DatabaseNotFound");

        var parentId = SqlContainerParentId(account.Name, databaseName);
        var existing = provider.GetSubresourceAs<SqlContainerResource>(sub, rg, collectionName, parentId, SqlContainersSubresource);
        if (existing != null)
            return new DataPlaneOperationResult<SqlContainerInnerResource>(OperationResult.Updated, existing.Properties.Resource, null, null);

        var request = new CreateOrUpdateSqlContainerRequest
        {
            Properties = new CreateOrUpdateSqlContainerRequest.CreateOrUpdateSqlContainerRequestProperties
            {
                Resource = new CreateOrUpdateSqlContainerRequest.CreateOrUpdateSqlContainerResourceInfo
                {
                    Id = collectionName,
                    PartitionKey = partitionKey,
                    IndexingPolicy = indexingPolicy,
                    UniqueKeyPolicy = uniqueKeyPolicy,
                    DefaultTtl = defaultTtl
                },
                Options = throughput.HasValue ? new SqlDatabaseOptions { Throughput = throughput } : null
            }
        };

        var properties = SqlContainerResourceProperties.FromRequest(collectionName, request);
        var resource = new SqlContainerResource(sub, rg, account.Name, databaseName, collectionName, properties);
        provider.CreateOrUpdateSubresource(sub, rg, collectionName, parentId, SqlContainersSubresource, resource);
        return new DataPlaneOperationResult<SqlContainerInnerResource>(OperationResult.Created, properties.Resource, null, null);
    }

    /// <summary>Returns the inner resource for a named collection, or NotFound if not found.</summary>
    internal DataPlaneOperationResult<SqlContainerInnerResource> GetCollection(HttpContext context, string databaseName, string collectionName)
    {
        var ctx = ResolveAccountContext(context);
        if (ctx == null) return new DataPlaneOperationResult<SqlContainerInnerResource>(OperationResult.NotFound, null, "Account not found.", "AccountNotFound");
        var (account, sub, rg) = ctx.Value;

        var parentId = SqlContainerParentId(account.Name, databaseName);
        var container = provider.GetSubresourceAs<SqlContainerResource>(sub, rg, collectionName, parentId, SqlContainersSubresource);
        return container == null
            ? new DataPlaneOperationResult<SqlContainerInnerResource>(OperationResult.NotFound, null, $"Collection '{collectionName}' not found.", "CollectionNotFound")
            : new DataPlaneOperationResult<SqlContainerInnerResource>(OperationResult.Success, container.Properties.Resource, null, null);
    }

    /// <summary>Deletes a named collection.</summary>
    internal DataPlaneOperationResult DeleteCollection(HttpContext context, string databaseName, string collectionName)
    {
        var ctx = ResolveAccountContext(context);
        if (ctx == null) return new DataPlaneOperationResult(OperationResult.NotFound, "Account not found.", "AccountNotFound");
        var (account, sub, rg) = ctx.Value;

        var parentId = SqlContainerParentId(account.Name, databaseName);
        var container = provider.GetSubresourceAs<SqlContainerResource>(sub, rg, collectionName, parentId, SqlContainersSubresource);
        if (container == null) return new DataPlaneOperationResult(OperationResult.NotFound, $"Collection '{collectionName}' not found.", "CollectionNotFound");

        provider.DeleteSubresource(sub, rg, collectionName, parentId, SqlContainersSubresource);
        return new DataPlaneOperationResult(OperationResult.Deleted, null, null);
    }

    /// <summary>Replaces an existing collection's indexing policy and TTL. Returns NotFound if the collection does not exist.</summary>
    internal DataPlaneOperationResult<SqlContainerInnerResource> ReplaceCollection(
        HttpContext context, string databaseName, string collectionName,
        object? indexingPolicy, int? defaultTtl)
    {
        var ctx = ResolveAccountContext(context);
        if (ctx == null) return new DataPlaneOperationResult<SqlContainerInnerResource>(OperationResult.NotFound, null, "Account not found.", "AccountNotFound");
        var (account, sub, rg) = ctx.Value;

        var parentId = SqlContainerParentId(account.Name, databaseName);
        var existing = provider.GetSubresourceAs<SqlContainerResource>(sub, rg, collectionName, parentId, SqlContainersSubresource);
        if (existing == null)
            return new DataPlaneOperationResult<SqlContainerInnerResource>(OperationResult.NotFound, null, $"Collection '{collectionName}' not found.", "CollectionNotFound");

        if (indexingPolicy != null) existing.Properties.Resource.IndexingPolicy = indexingPolicy;
        if (defaultTtl.HasValue) existing.Properties.Resource.DefaultTtl = defaultTtl;
        existing.Properties.Resource.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        provider.CreateOrUpdateSubresource(sub, rg, collectionName, parentId, SqlContainersSubresource, existing);
        return new DataPlaneOperationResult<SqlContainerInnerResource>(OperationResult.Updated, existing.Properties.Resource, null, null);
    }

    /// <summary>Lists all collections for the given database resolved from the request host.</summary>
    internal DataPlaneOperationResult<SqlContainerInnerResource[]> ListCollections(HttpContext context, string databaseName)
    {
        var ctx = ResolveAccountContext(context);
        if (ctx == null) return new DataPlaneOperationResult<SqlContainerInnerResource[]>(OperationResult.NotFound, null, "Account not found.", "AccountNotFound");
        var (account, sub, rg) = ctx.Value;

        var database = provider.GetSubresourceAs<SqlDatabaseResource>(sub, rg, databaseName, account.Name, SqlDatabasesSubresource);
        if (database == null) return new DataPlaneOperationResult<SqlContainerInnerResource[]>(OperationResult.NotFound, null, $"Database '{databaseName}' not found.", "DatabaseNotFound");

        var parentId = SqlContainerParentId(account.Name, databaseName);
        var containers = provider.ListSubresourcesAs<SqlContainerResource>(sub, rg, parentId, SqlContainersSubresource);
        return new DataPlaneOperationResult<SqlContainerInnerResource[]>(OperationResult.Success, containers.Select(c => c.Properties.Resource).ToArray(), null, null);
    }

    private static string DocFileName(string docId) =>
        Uri.EscapeDataString(docId) + ".json";

    private static string? ExtractPartitionKeyValue(JsonObject doc, SqlContainerInnerResource container)
    {
        // PartitionKey.Paths is e.g. ["/pk"]; we only look at the first path for v1.7
        var path = container.PartitionKey?.Paths?.FirstOrDefault();
        if (string.IsNullOrEmpty(path)) return null;

        var field = path.TrimStart('/');
        return doc[field]?.ToString();
    }

    private string? ParsePartitionKeyHeader(string header)
    {
        // Header value is a JSON array, e.g. ["value"] or [null]
        try
        {
            var arr = JsonNode.Parse(header)?.AsArray();
            return arr?[0]?.ToString();
        }
        catch (Exception ex)
        {
            logger.LogDebug(nameof(CosmosDbDataPlane), nameof(ParsePartitionKeyHeader),
                "Failed to parse x-ms-documentdb-partitionkey header '{0}': {1}", header, ex.Message);
            return null;
        }
    }

    /// <summary>Creates a new document in the given collection.</summary>
    internal DataPlaneOperationResult<JsonObject> CreateDocument(
        HttpContext context, string databaseName, string collectionName, JsonObject body)
    {
        var ctx = ResolveAccountContext(context);
        if (ctx == null) return new DataPlaneOperationResult<JsonObject>(OperationResult.NotFound, null, "Account not found.", "AccountNotFound");
        var (account, sub, rg) = ctx.Value;

        var parentId = SqlContainerParentId(account.Name, databaseName);
        var container = provider.GetSubresourceAs<SqlContainerResource>(sub, rg, collectionName, parentId, SqlContainersSubresource);
        if (container == null) return new DataPlaneOperationResult<JsonObject>(OperationResult.NotFound, null, $"Collection '{collectionName}' not found.", "CollectionNotFound");

        var docId = body["id"]?.ToString();
        if (string.IsNullOrWhiteSpace(docId))
            return new DataPlaneOperationResult<JsonObject>(OperationResult.BadRequest, null, "Document must have an 'id' field.", "BadRequest");

        // Validate partition key is present in the document body
        var pkValue = ExtractPartitionKeyValue(body, container.Properties.Resource);
        var pkPath = container.Properties.Resource.PartitionKey?.Paths?.FirstOrDefault();
        if (!string.IsNullOrEmpty(pkPath) && pkValue == null)
            return new DataPlaneOperationResult<JsonObject>(OperationResult.BadRequest, null,
                $"The partition key path '{pkPath}' was not present in the document.", "BadRequest");

        var docsDir = provider.GetDocumentDirectory(sub, rg, account.Name, databaseName, collectionName);
        var filePath = Path.Combine(docsDir, DocFileName(docId));
        if (File.Exists(filePath))
            return new DataPlaneOperationResult<JsonObject>(OperationResult.Conflict, null, $"Document with id '{docId}' already exists.", "Conflict");

        var doc = DocumentItemResource.Create(body, databaseName, collectionName);
        File.WriteAllText(filePath, doc.ToJsonString());

        return new DataPlaneOperationResult<JsonObject>(OperationResult.Created, doc, null, null);
    }

    /// <summary>Reads a single document by id. Validates the partition key header.</summary>
    internal DataPlaneOperationResult<JsonObject> GetDocument(
        HttpContext context, string databaseName, string collectionName, string docId, string partitionKeyHeader)
    {
        var ctx = ResolveAccountContext(context);
        if (ctx == null) return new DataPlaneOperationResult<JsonObject>(OperationResult.NotFound, null, "Account not found.", "AccountNotFound");
        var (account, sub, rg) = ctx.Value;

        var parentId = SqlContainerParentId(account.Name, databaseName);
        var container = provider.GetSubresourceAs<SqlContainerResource>(sub, rg, collectionName, parentId, SqlContainersSubresource);
        if (container == null) return new DataPlaneOperationResult<JsonObject>(OperationResult.NotFound, null, $"Collection '{collectionName}' not found.", "CollectionNotFound");

        var docsDir = provider.GetDocumentDirectory(sub, rg, account.Name, databaseName, collectionName);
        var filePath = Path.Combine(docsDir, DocFileName(docId));
        if (!File.Exists(filePath))
            return new DataPlaneOperationResult<JsonObject>(OperationResult.NotFound, null, $"Document '{docId}' not found.", "NotFound");

        var doc = JsonNode.Parse(File.ReadAllText(filePath))!.AsObject();

        // Validate partition key header
        var headerPkValue = ParsePartitionKeyHeader(partitionKeyHeader);
        var storedPkValue = ExtractPartitionKeyValue(doc, container.Properties.Resource);
        if (storedPkValue != null && headerPkValue != storedPkValue)
            return new DataPlaneOperationResult<JsonObject>(OperationResult.BadRequest, null,
                "The partition key value in the request header does not match the stored document.", "BadRequest");

        return new DataPlaneOperationResult<JsonObject>(OperationResult.Success, doc, null, null);
    }

    /// <summary>Fully replaces a document. Respects <c>If-Match</c> ETag.</summary>
    internal DataPlaneOperationResult<JsonObject> ReplaceDocument(
        HttpContext context, string databaseName, string collectionName, string docId,
        JsonObject body, string? ifMatchEtag)
    {
        var ctx = ResolveAccountContext(context);
        if (ctx == null) return new DataPlaneOperationResult<JsonObject>(OperationResult.NotFound, null, "Account not found.", "AccountNotFound");
        var (account, sub, rg) = ctx.Value;

        var docsDir = provider.GetDocumentDirectory(sub, rg, account.Name, databaseName, collectionName);
        var filePath = Path.Combine(docsDir, DocFileName(docId));
        if (!File.Exists(filePath))
            return new DataPlaneOperationResult<JsonObject>(OperationResult.NotFound, null, $"Document '{docId}' not found.", "NotFound");

        var existing = JsonNode.Parse(File.ReadAllText(filePath))!.AsObject();

        // ETag concurrency check
        if (!string.IsNullOrEmpty(ifMatchEtag) && ifMatchEtag != "*")
        {
            var storedEtag = existing["_etag"]?.ToString();
            if (storedEtag != ifMatchEtag)
                return new DataPlaneOperationResult<JsonObject>(OperationResult.PreconditionFailed, null,
                    "The operation specified an If-Match condition that is not satisfied.", "PreconditionFailed");
        }

        // Full replace: user body with preserved _rid/_self and fresh _etag/_ts
        var doc = JsonNode.Parse(body.ToJsonString())!.AsObject();
        doc["_rid"] = existing["_rid"]?.DeepClone();
        doc["_self"] = existing["_self"]?.DeepClone();
        DocumentItemResource.RefreshSystemFields(doc);

        File.WriteAllText(filePath, doc.ToJsonString());
        return new DataPlaneOperationResult<JsonObject>(OperationResult.Updated, doc, null, null);
    }

    /// <summary>Applies Cosmos DB patch operations to a document. Respects <c>If-Match</c> ETag.</summary>
    internal DataPlaneOperationResult<JsonObject> PatchDocument(
        HttpContext context, string databaseName, string collectionName, string docId,
        PatchDocumentRequest patchRequest, string? ifMatchEtag)
    {
        var ctx = ResolveAccountContext(context);
        if (ctx == null) return new DataPlaneOperationResult<JsonObject>(OperationResult.NotFound, null, "Account not found.", "AccountNotFound");
        var (account, sub, rg) = ctx.Value;

        var docsDir = provider.GetDocumentDirectory(sub, rg, account.Name, databaseName, collectionName);
        var filePath = Path.Combine(docsDir, DocFileName(docId));
        if (!File.Exists(filePath))
            return new DataPlaneOperationResult<JsonObject>(OperationResult.NotFound, null, $"Document '{docId}' not found.", "NotFound");

        var doc = JsonNode.Parse(File.ReadAllText(filePath))!.AsObject();

        // ETag concurrency check
        if (!string.IsNullOrEmpty(ifMatchEtag) && ifMatchEtag != "*")
        {
            var storedEtag = doc["_etag"]?.ToString();
            if (storedEtag != ifMatchEtag)
                return new DataPlaneOperationResult<JsonObject>(OperationResult.PreconditionFailed, null,
                    "The operation specified an If-Match condition that is not satisfied.", "PreconditionFailed");
        }

        foreach (var op in patchRequest.Operations)
        {
            var field = op.Path.TrimStart('/');
            switch (op.Op.ToLowerInvariant())
            {
                case "set":
                case "add":
                case "replace":
                    doc[field] = op.Value?.DeepClone();
                    break;
                case "remove":
                    doc.Remove(field);
                    break;
                case "incr":
                case "increment":
                {
                    var delta = op.Value?.GetValue<double>() ?? 0;
                    var current = doc[field]?.GetValue<double>() ?? 0;
                    doc[field] = current + delta;
                    break;
                }
                default:
                    logger.LogDebug(nameof(CosmosDbDataPlane), nameof(PatchDocument),
                        "Unknown patch op '{0}' — ignored.", op.Op);
                    break;
            }
        }

        DocumentItemResource.RefreshSystemFields(doc);
        File.WriteAllText(filePath, doc.ToJsonString());
        return new DataPlaneOperationResult<JsonObject>(OperationResult.Updated, doc, null, null);
    }

    /// <summary>Deletes a document. Validates the partition key header and respects <c>If-Match</c>.</summary>
    internal DataPlaneOperationResult DeleteDocument(
        HttpContext context, string databaseName, string collectionName, string docId,
        string partitionKeyHeader, string? ifMatchEtag)
    {
        var ctx = ResolveAccountContext(context);
        if (ctx == null) return new DataPlaneOperationResult(OperationResult.NotFound, "Account not found.", "AccountNotFound");
        var (account, sub, rg) = ctx.Value;

        var parentId = SqlContainerParentId(account.Name, databaseName);
        var container = provider.GetSubresourceAs<SqlContainerResource>(sub, rg, collectionName, parentId, SqlContainersSubresource);
        if (container == null) return new DataPlaneOperationResult(OperationResult.NotFound, $"Collection '{collectionName}' not found.", "CollectionNotFound");

        var docsDir = provider.GetDocumentDirectory(sub, rg, account.Name, databaseName, collectionName);
        var filePath = Path.Combine(docsDir, DocFileName(docId));
        if (!File.Exists(filePath))
            return new DataPlaneOperationResult(OperationResult.NotFound, $"Document '{docId}' not found.", "NotFound");

        var doc = JsonNode.Parse(File.ReadAllText(filePath))!.AsObject();

        // Validate partition key header
        var headerPkValue = ParsePartitionKeyHeader(partitionKeyHeader);
        var storedPkValue = ExtractPartitionKeyValue(doc, container.Properties.Resource);
        if (storedPkValue != null && headerPkValue != storedPkValue)
            return new DataPlaneOperationResult(OperationResult.BadRequest,
                "The partition key value in the request header does not match the stored document.", "BadRequest");

        // ETag concurrency check
        if (!string.IsNullOrEmpty(ifMatchEtag) && ifMatchEtag != "*")
        {
            var storedEtag = doc["_etag"]?.ToString();
            if (storedEtag != ifMatchEtag)
                return new DataPlaneOperationResult(OperationResult.PreconditionFailed,
                    "The operation specified an If-Match condition that is not satisfied.", "PreconditionFailed");
        }

        File.Delete(filePath);
        return new DataPlaneOperationResult(OperationResult.Deleted, null, null);
    }

    /// <summary>
    /// Executes a Cosmos DB SQL query against all documents in the collection and
    /// returns the projected page together with an optional continuation-token skip offset.
    /// </summary>
    internal DataPlaneOperationResult<QueryDocumentsResponse> QueryDocuments(
        HttpContext context,
        string databaseName,
        string collectionName,
        CosmosDbSqlQueryRequest request,
        int maxItemCount,
        int skip)
    {
        var ctx = ResolveAccountContext(context);
        if (ctx == null)
            return new DataPlaneOperationResult<QueryDocumentsResponse>(
                OperationResult.NotFound, null, "Account not found.", "AccountNotFound");

        var (account, sub, rg) = ctx.Value;

        var parentId = SqlContainerParentId(account.Name, databaseName);
        var container = provider.GetSubresourceAs<SqlContainerResource>(
            sub, rg, collectionName, parentId, SqlContainersSubresource);
        if (container == null)
            return new DataPlaneOperationResult<QueryDocumentsResponse>(
                OperationResult.NotFound, null,
                $"Collection '{collectionName}' not found.", "CollectionNotFound");

        ParsedQuery query;
        try
        {
            query = new CosmosDbSqlParser().Parse(request.Query);
        }
        catch (Exception ex)
        {
            logger.LogDebug(nameof(CosmosDbDataPlane), nameof(QueryDocuments),
                "Failed to parse query '{0}': {1}", request.Query, ex.Message);
            return new DataPlaneOperationResult<QueryDocumentsResponse>(
                OperationResult.BadRequest, null,
                $"Invalid query syntax: {ex.Message}", "BadRequest");
        }

        var parameters = (request.Parameters ?? [])
            .ToDictionary(p => p.Name, p => p.Value);

        var docsDir = provider.GetDocumentDirectory(sub, rg, account.Name, databaseName, collectionName);
        var files = Directory.GetFiles(docsDir, "*.json", SearchOption.TopDirectoryOnly);
        var docs = files
            .Select(f =>
            {
                try { return JsonNode.Parse(File.ReadAllText(f))?.AsObject(); }
                catch (Exception ex)
                {
                    logger.LogDebug(nameof(CosmosDbDataPlane), nameof(QueryDocuments),
                        "Failed to read document file '{0}': {1}", f, ex.Message);
                    return null;
                }
            })
            .Where(d => d != null)
            .Select(d => d!)
            .ToArray();

        var result = new CosmosDbSqlExecutor().Execute(docs, query, parameters, skip, maxItemCount);

        var response = new QueryDocumentsResponse
        {
            Rid = container.Properties.Resource.Rid,
            Documents = result.Results,
            Count = result.Results.Length,
            NextSkip = result.NextSkip
        };

        return new DataPlaneOperationResult<QueryDocumentsResponse>(
            OperationResult.Success, response, null, null);
    }

    /// <summary>Lists all documents in a collection (full scan, no pagination).</summary>
    internal DataPlaneOperationResult<JsonObject[]> ListDocuments(
        HttpContext context, string databaseName, string collectionName)
    {
        var ctx = ResolveAccountContext(context);
        if (ctx == null) return new DataPlaneOperationResult<JsonObject[]>(OperationResult.NotFound, null, "Account not found.", "AccountNotFound");
        var (account, sub, rg) = ctx.Value;

        var parentId = SqlContainerParentId(account.Name, databaseName);
        var container = provider.GetSubresourceAs<SqlContainerResource>(sub, rg, collectionName, parentId, SqlContainersSubresource);
        if (container == null) return new DataPlaneOperationResult<JsonObject[]>(OperationResult.NotFound, null, $"Collection '{collectionName}' not found.", "CollectionNotFound");

        var docsDir = provider.GetDocumentDirectory(sub, rg, account.Name, databaseName, collectionName);
        var files = Directory.GetFiles(docsDir, "*.json", SearchOption.TopDirectoryOnly);

        var docs = files
            .Select(f =>
            {
                try { return JsonNode.Parse(File.ReadAllText(f))?.AsObject(); }
                catch (Exception ex)
                {
                    logger.LogDebug(nameof(CosmosDbDataPlane), nameof(ListDocuments),
                        "Failed to read document file '{0}': {1}", f, ex.Message);
                    return null;
                }
            })
            .Where(d => d != null)
            .Select(d => d!)
            .ToArray();

        return new DataPlaneOperationResult<JsonObject[]>(OperationResult.Success, docs, null, null);
    }
}
