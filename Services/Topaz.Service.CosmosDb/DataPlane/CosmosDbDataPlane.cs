using System.Linq;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.CosmosDb.Models;
using Topaz.Service.CosmosDb.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.DataPlane;

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
}
