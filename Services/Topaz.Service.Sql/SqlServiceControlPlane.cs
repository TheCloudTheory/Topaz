using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Sql.Models;
using Topaz.Service.Sql.Models.Requests;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.Sql;


internal sealed class SqlServiceControlPlane(
    Pipeline eventPipeline,
    SqlServerResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string SqlServerNotFoundCode = "ResourceNotFound";
    private const string SqlServerNotFoundMessageTemplate = "SQL server '{0}' could not be found";
    private const string SqlDatabaseNotFoundCode = "ResourceNotFound";
    private const string SqlDatabaseNotFoundMessageTemplate = "SQL database '{0}' could not be found on server '{1}'";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public static SqlServiceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new SqlServerResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var server = resource.As<SqlServerResource, SqlServerResourceProperties>();
        if (server == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a SQL Server instance.");
            return OperationResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(server.Location))
        {
            logger.LogError($"SQL server resource `{resource.Id}` is missing required location.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(
                server.GetSubscription(),
                server.GetResourceGroup(),
                server.Name,
                new CreateOrUpdateSqlServerRequest
                {
                    Location = server.Location,
                    Tags = server.Tags,
                    Properties = new CreateOrUpdateSqlServerRequest.CreateOrUpdateSqlServerRequestProperties
                    {
                        AdministratorLogin = server.Properties.AdministratorLogin,
                        AdministratorLoginPassword = server.Properties.AdministratorLoginPassword,
                        Version = server.Properties.Version,
                        PublicNetworkAccess = server.Properties.PublicNetworkAccess
                    }
                });

            return result.Result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            return OperationResult.Failed;
        }
    }

    public ControlPlaneOperationResult<SqlServerResource> Get(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string serverName)
    {
        var resource = provider.GetAs<SqlServerResource>(subscriptionIdentifier, resourceGroupIdentifier, serverName);

        return resource == null
            ? new ControlPlaneOperationResult<SqlServerResource>(
                OperationResult.NotFound,
                null,
                string.Format(SqlServerNotFoundMessageTemplate, serverName),
                SqlServerNotFoundCode)
            : new ControlPlaneOperationResult<SqlServerResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<SqlServerResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string serverName,
        CreateOrUpdateSqlServerRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<SqlServerResource>(
                OperationResult.NotFound,
                null,
                resourceGroupOperation.Reason,
                resourceGroupOperation.Code);
        }

        var existing = provider.GetAs<SqlServerResource>(subscriptionIdentifier, resourceGroupIdentifier, serverName);

        if (existing != null)
        {
            existing.Location = request.Location ?? existing.Location;
            existing.Tags = request.Tags ?? existing.Tags;
            SqlServerResourceProperties.UpdateFromRequest(existing.Properties, request);
            provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, serverName, existing);

            return new ControlPlaneOperationResult<SqlServerResource>(OperationResult.Updated, existing, null, null);
        }

        var location = request.Location ?? resourceGroupOperation.Resource!.Location!;
        var properties = SqlServerResourceProperties.FromRequest(serverName, request);
        var resource = new SqlServerResource(
            subscriptionIdentifier,
            resourceGroupIdentifier,
            serverName,
            location,
            request.Tags,
            properties);

        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, serverName, resource);

        return new ControlPlaneOperationResult<SqlServerResource>(OperationResult.Created, resource, null, null);
    }

    public ControlPlaneOperationResult<SqlServerResource> Delete(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string serverName)
    {
        var existing = provider.GetAs<SqlServerResource>(subscriptionIdentifier, resourceGroupIdentifier, serverName);

        if (existing == null)
        {
            return new ControlPlaneOperationResult<SqlServerResource>(
                OperationResult.NotFound,
                null,
                string.Format(SqlServerNotFoundMessageTemplate, serverName),
                SqlServerNotFoundCode);
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, serverName);

        return new ControlPlaneOperationResult<SqlServerResource>(OperationResult.Deleted, existing, null, null);
    }

    public IEnumerable<SqlServerResource> ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier)
    {
        return provider.ListAs<SqlServerResource>(subscriptionIdentifier, resourceGroupIdentifier,
            lookForNoOfSegments: 8);
    }

    public IEnumerable<SqlServerResource> ListBySubscription(SubscriptionIdentifier subscriptionIdentifier)
    {
        return provider.ListAs<SqlServerResource>(subscriptionIdentifier, null,
            lookForNoOfSegments: 8);
    }

    public ControlPlaneOperationResult<SqlDatabaseResource> CreateOrUpdateDatabase(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string serverName,
        string databaseName,
        CreateOrUpdateSqlDatabaseRequest request)
    {
        var serverOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, serverName);
        if (serverOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<SqlDatabaseResource>(
                OperationResult.NotFound,
                null,
                serverOperation.Reason,
                serverOperation.Code);
        }

        var subresource = nameof(Subresource.Databases).ToLowerInvariant();
        var existing = provider.GetSubresourceAs<SqlDatabaseResource>(
            subscriptionIdentifier, resourceGroupIdentifier, databaseName, serverName, subresource);

        if (existing != null)
        {
            existing.Location = request.Location ?? existing.Location;
            existing.Tags = request.Tags ?? existing.Tags;
            SqlDatabaseResourceProperties.UpdateFromRequest(existing.Properties, request);
            provider.CreateOrUpdateSubresource(
                subscriptionIdentifier, resourceGroupIdentifier, databaseName, serverName, subresource, existing);

            return new ControlPlaneOperationResult<SqlDatabaseResource>(OperationResult.Updated, existing, null, null);
        }

        var location = request.Location ?? serverOperation.Resource!.Location!;
        var properties = SqlDatabaseResourceProperties.FromRequest(request);
        var database = new SqlDatabaseResource(
            subscriptionIdentifier,
            resourceGroupIdentifier,
            serverName,
            databaseName,
            location,
            request.Tags,
            request.Sku,
            properties);

        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, databaseName, serverName, subresource, database);

        return new ControlPlaneOperationResult<SqlDatabaseResource>(OperationResult.Created, database, null, null);
    }

    public ControlPlaneOperationResult<SqlDatabaseResource> GetDatabase(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string serverName,
        string databaseName)
    {
        var subresource = nameof(Subresource.Databases).ToLowerInvariant();
        var database = provider.GetSubresourceAs<SqlDatabaseResource>(
            subscriptionIdentifier, resourceGroupIdentifier, databaseName, serverName, subresource);

        return database == null
            ? new ControlPlaneOperationResult<SqlDatabaseResource>(
                OperationResult.NotFound,
                null,
                string.Format(SqlDatabaseNotFoundMessageTemplate, databaseName, serverName),
                SqlDatabaseNotFoundCode)
            : new ControlPlaneOperationResult<SqlDatabaseResource>(OperationResult.Success, database, null, null);
    }

    public ControlPlaneOperationResult<SqlDatabaseResource> DeleteDatabase(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string serverName,
        string databaseName)
    {
        var subresource = nameof(Subresource.Databases).ToLowerInvariant();
        var existing = provider.GetSubresourceAs<SqlDatabaseResource>(
            subscriptionIdentifier, resourceGroupIdentifier, databaseName, serverName, subresource);

        if (existing == null)
        {
            return new ControlPlaneOperationResult<SqlDatabaseResource>(
                OperationResult.NotFound,
                null,
                string.Format(SqlDatabaseNotFoundMessageTemplate, databaseName, serverName),
                SqlDatabaseNotFoundCode);
        }

        provider.DeleteSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, databaseName, serverName, subresource);

        return new ControlPlaneOperationResult<SqlDatabaseResource>(OperationResult.Deleted, existing, null, null);
    }

    public IEnumerable<SqlDatabaseResource> ListDatabases(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string serverName)
    {
        var subresource = nameof(Subresource.Databases).ToLowerInvariant();
        return provider.ListSubresourcesAs<SqlDatabaseResource>(
            subscriptionIdentifier, resourceGroupIdentifier, serverName, subresource);
    }
}
