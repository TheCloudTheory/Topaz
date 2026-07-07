using System.Text.Json.Nodes;
using Topaz.Service.CosmosDb.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.CosmosDb;

internal interface ICosmosDbDataPlane
{
    DataPlaneOperationResult<SqlDatabaseInnerResource[]> ListDatabases(CosmosDbAccountContext ctx);
    DataPlaneOperationResult<SqlContainerInnerResource[]> ListCollections(CosmosDbAccountContext ctx, string databaseName);
    DataPlaneOperationResult<JsonObject[]> ListDocuments(CosmosDbAccountContext ctx, string databaseName, string collectionName);
    DataPlaneOperationResult DeleteDocument(CosmosDbAccountContext ctx, string databaseName, string collectionName, string docId, string partitionKeyHeader, string? ifMatchEtag);
}
