using System.Security.Cryptography;
using Azure.Core;
using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.CosmosDb.Models;
using Topaz.Service.CosmosDb.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Dns;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb;

internal sealed class CosmosDbServiceControlPlane(
    Pipeline eventPipeline,
    DatabaseAccountResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string DatabaseAccountNotFoundCode = "DatabaseAccountNotFound";
    private const string DatabaseAccountNotFoundMessageTemplate = "Database account '{0}' could not be found.";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public static CosmosDbServiceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new DatabaseAccountResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var account = resource.As<DatabaseAccountResource, DatabaseAccountResourceProperties>();
        if (account == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Cosmos DB database account.");
            return OperationResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(account.Location))
        {
            logger.LogError($"Cosmos DB database account resource `{resource.Id}` is missing required location.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(
                account.GetSubscription(),
                account.GetResourceGroup(),
                account.Name,
                CreateOrUpdateDatabaseAccountRequest.FromResource(account));

            return result.Result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            return OperationResult.Failed;
        }
    }

    public ControlPlaneOperationResult<DatabaseAccountResource> Get(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName)
    {
        var resource = provider.GetAs<DatabaseAccountResource>(subscriptionIdentifier, resourceGroupIdentifier, accountName);

        return resource == null
            ? new ControlPlaneOperationResult<DatabaseAccountResource>(
                OperationResult.NotFound,
                null,
                string.Format(DatabaseAccountNotFoundMessageTemplate, accountName),
                DatabaseAccountNotFoundCode)
            : new ControlPlaneOperationResult<DatabaseAccountResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<DatabaseAccountResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName,
        CreateOrUpdateDatabaseAccountRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<DatabaseAccountResource>(
                OperationResult.NotFound,
                null,
                resourceGroupOperation.Reason,
                resourceGroupOperation.Code);
        }

        var existing = provider.GetAs<DatabaseAccountResource>(subscriptionIdentifier, resourceGroupIdentifier, accountName);

        if (existing != null)
        {
            existing.Location = request.Location?.ToString() ?? existing.Location;
            existing.Tags = request.Tags ?? existing.Tags;
            DatabaseAccountResourceProperties.UpdateFromRequest(existing.Properties, request);
            provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, accountName, existing);

            return new ControlPlaneOperationResult<DatabaseAccountResource>(OperationResult.Updated, existing, null, null);
        }

        var location = new AzureLocation(request.Location ?? resourceGroupOperation.Resource!.Location!);
        var properties = DatabaseAccountResourceProperties.FromRequest(accountName, request);
        var resource = new DatabaseAccountResource(subscriptionIdentifier, resourceGroupIdentifier, accountName, location, request.Tags, properties);

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, accountName, resource, createOperation: true);

        return new ControlPlaneOperationResult<DatabaseAccountResource>(OperationResult.Created, resource, null, null);
    }

    public ControlPlaneOperationResult Delete(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName)
    {
        var resource = provider.GetAs<DatabaseAccountResource>(subscriptionIdentifier, resourceGroupIdentifier, accountName);

        if (resource == null)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                string.Format(DatabaseAccountNotFoundMessageTemplate, accountName),
                DatabaseAccountNotFoundCode);
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, accountName);

        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<DatabaseAccountResource[]> ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources = provider.ListAs<DatabaseAccountResource>(subscriptionIdentifier, resourceGroupIdentifier,
                lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(subscriptionIdentifier) && r.IsInResourceGroup(resourceGroupIdentifier))
            .ToArray();

        return new ControlPlaneOperationResult<DatabaseAccountResource[]>(OperationResult.Success, resources, null, null);
    }

    public ControlPlaneOperationResult<DatabaseAccountResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<DatabaseAccountResource>(subscriptionIdentifier, null,
                lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(subscriptionIdentifier))
            .ToArray();

        return new ControlPlaneOperationResult<DatabaseAccountResource[]>(OperationResult.Success, resources, null, null);
    }

    public ControlPlaneOperationResult<SqlDatabaseResource> CreateOrUpdateSqlDatabase(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName,
        string databaseName,
        CreateOrUpdateSqlDatabaseRequest request)
    {
        var account = provider.GetAs<DatabaseAccountResource>(subscriptionIdentifier, resourceGroupIdentifier, accountName);
        if (account == null)
        {
            return new ControlPlaneOperationResult<SqlDatabaseResource>(
                OperationResult.NotFound, null,
                string.Format(DatabaseAccountNotFoundMessageTemplate, accountName),
                DatabaseAccountNotFoundCode);
        }

        var existing = provider.GetSubresourceAs<SqlDatabaseResource>(
            subscriptionIdentifier, resourceGroupIdentifier, databaseName, accountName,
            nameof(Subresource.SqlDatabases).ToLowerInvariant());

        if (existing != null)
        {
            SqlDatabaseResourceProperties.UpdateFromRequest(existing.Properties, request);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, databaseName,
                accountName, nameof(Subresource.SqlDatabases).ToLowerInvariant(), existing);
            return new ControlPlaneOperationResult<SqlDatabaseResource>(OperationResult.Updated, existing, null, null);
        }

        var properties = SqlDatabaseResourceProperties.FromRequest(databaseName, request);
        var resource = new SqlDatabaseResource(subscriptionIdentifier, resourceGroupIdentifier, accountName, databaseName, properties);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, databaseName,
            accountName, nameof(Subresource.SqlDatabases).ToLowerInvariant(), resource);
        return new ControlPlaneOperationResult<SqlDatabaseResource>(OperationResult.Created, resource, null, null);
    }

    public ControlPlaneOperationResult<SqlDatabaseResource> GetSqlDatabase(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName,
        string databaseName)
    {
        var database = provider.GetSubresourceAs<SqlDatabaseResource>(
            subscriptionIdentifier, resourceGroupIdentifier, databaseName, accountName,
            nameof(Subresource.SqlDatabases).ToLowerInvariant());

        return database == null
            ? new ControlPlaneOperationResult<SqlDatabaseResource>(
                OperationResult.NotFound, null,
                $"SQL database '{databaseName}' could not be found under account '{accountName}'.",
                "SqlDatabaseNotFound")
            : new ControlPlaneOperationResult<SqlDatabaseResource>(OperationResult.Success, database, null, null);
    }

    public ControlPlaneOperationResult DeleteSqlDatabase(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName,
        string databaseName)
    {
        var database = provider.GetSubresourceAs<SqlDatabaseResource>(
            subscriptionIdentifier, resourceGroupIdentifier, databaseName, accountName,
            nameof(Subresource.SqlDatabases).ToLowerInvariant());

        if (database == null)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                $"SQL database '{databaseName}' could not be found under account '{accountName}'.",
                "SqlDatabaseNotFound");
        }

        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, databaseName,
            accountName, nameof(Subresource.SqlDatabases).ToLowerInvariant());
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<SqlDatabaseResource[]> ListSqlDatabases(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName)
    {
        var account = provider.GetAs<DatabaseAccountResource>(subscriptionIdentifier, resourceGroupIdentifier, accountName);
        if (account == null)
        {
            return new ControlPlaneOperationResult<SqlDatabaseResource[]>(
                OperationResult.NotFound, null,
                string.Format(DatabaseAccountNotFoundMessageTemplate, accountName),
                DatabaseAccountNotFoundCode);
        }

        var databases = provider.ListSubresourcesAs<SqlDatabaseResource>(
            subscriptionIdentifier, resourceGroupIdentifier, accountName,
            nameof(Subresource.SqlDatabases).ToLowerInvariant());

        return new ControlPlaneOperationResult<SqlDatabaseResource[]>(OperationResult.Success, databases, null, null);
    }

    public ControlPlaneOperationResult<SqlDatabaseThroughputSettingsResponse> GetSqlDatabaseThroughput(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName,
        string databaseName)
    {
        var database = provider.GetSubresourceAs<SqlDatabaseResource>(
            subscriptionIdentifier, resourceGroupIdentifier, databaseName, accountName,
            nameof(Subresource.SqlDatabases).ToLowerInvariant());

        if (database == null)
        {
            return new ControlPlaneOperationResult<SqlDatabaseThroughputSettingsResponse>(
                OperationResult.NotFound, null,
                $"SQL database '{databaseName}' could not be found under account '{accountName}'.",
                "SqlDatabaseNotFound");
        }

        return new ControlPlaneOperationResult<SqlDatabaseThroughputSettingsResponse>(
            OperationResult.Success, SqlDatabaseThroughputSettingsResponse.From(database), null, null);
    }

    public ControlPlaneOperationResult<SqlDatabaseThroughputSettingsResponse> UpdateSqlDatabaseThroughput(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName,
        string databaseName,
        UpdateSqlDatabaseThroughputRequest request)
    {
        var database = provider.GetSubresourceAs<SqlDatabaseResource>(
            subscriptionIdentifier, resourceGroupIdentifier, databaseName, accountName,
            nameof(Subresource.SqlDatabases).ToLowerInvariant());

        if (database == null)
        {
            return new ControlPlaneOperationResult<SqlDatabaseThroughputSettingsResponse>(
                OperationResult.NotFound, null,
                $"SQL database '{databaseName}' could not be found under account '{accountName}'.",
                "SqlDatabaseNotFound");
        }

        SqlDatabaseResourceProperties.UpdateThroughputFromRequest(database.Properties, request);

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, databaseName,
            accountName, nameof(Subresource.SqlDatabases).ToLowerInvariant(), database);

        return new ControlPlaneOperationResult<SqlDatabaseThroughputSettingsResponse>(
            OperationResult.Success, SqlDatabaseThroughputSettingsResponse.From(database), null, null);
    }

    private static string SqlContainerParentId(string accountName, string databaseName) =>
        $"{accountName}/{nameof(Subresource.SqlDatabases).ToLowerInvariant()}/{databaseName}";

    public ControlPlaneOperationResult<SqlContainerResource> CreateOrUpdateSqlContainer(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName,
        string databaseName,
        string containerName,
        CreateOrUpdateSqlContainerRequest request)
    {
        var account = provider.GetAs<DatabaseAccountResource>(subscriptionIdentifier, resourceGroupIdentifier, accountName);
        if (account == null)
        {
            return new ControlPlaneOperationResult<SqlContainerResource>(
                OperationResult.NotFound, null,
                string.Format(DatabaseAccountNotFoundMessageTemplate, accountName),
                DatabaseAccountNotFoundCode);
        }

        var parentId = SqlContainerParentId(accountName, databaseName);
        var existing = provider.GetSubresourceAs<SqlContainerResource>(
            subscriptionIdentifier, resourceGroupIdentifier, containerName, parentId,
            nameof(Subresource.SqlContainers).ToLowerInvariant());

        if (existing != null)
        {
            SqlContainerResourceProperties.UpdateFromRequest(existing.Properties, request);
            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, containerName,
                parentId, nameof(Subresource.SqlContainers).ToLowerInvariant(), existing);
            return new ControlPlaneOperationResult<SqlContainerResource>(OperationResult.Updated, existing, null, null);
        }

        var properties = SqlContainerResourceProperties.FromRequest(containerName, request);
        var resource = new SqlContainerResource(subscriptionIdentifier, resourceGroupIdentifier, accountName, databaseName, containerName, properties);
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, containerName,
            parentId, nameof(Subresource.SqlContainers).ToLowerInvariant(), resource);
        return new ControlPlaneOperationResult<SqlContainerResource>(OperationResult.Created, resource, null, null);
    }

    public ControlPlaneOperationResult<SqlContainerResource> GetSqlContainer(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName,
        string databaseName,
        string containerName)
    {
        var parentId = SqlContainerParentId(accountName, databaseName);
        var container = provider.GetSubresourceAs<SqlContainerResource>(
            subscriptionIdentifier, resourceGroupIdentifier, containerName, parentId,
            nameof(Subresource.SqlContainers).ToLowerInvariant());

        return container == null
            ? new ControlPlaneOperationResult<SqlContainerResource>(
                OperationResult.NotFound, null,
                $"SQL container '{containerName}' could not be found under database '{databaseName}'.",
                "SqlContainerNotFound")
            : new ControlPlaneOperationResult<SqlContainerResource>(OperationResult.Success, container, null, null);
    }

    public ControlPlaneOperationResult DeleteSqlContainer(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName,
        string databaseName,
        string containerName)
    {
        var parentId = SqlContainerParentId(accountName, databaseName);
        var container = provider.GetSubresourceAs<SqlContainerResource>(
            subscriptionIdentifier, resourceGroupIdentifier, containerName, parentId,
            nameof(Subresource.SqlContainers).ToLowerInvariant());

        if (container == null)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                $"SQL container '{containerName}' could not be found under database '{databaseName}'.",
                "SqlContainerNotFound");
        }

        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, containerName,
            parentId, nameof(Subresource.SqlContainers).ToLowerInvariant());
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<SqlContainerResource[]> ListSqlContainers(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName,
        string databaseName)
    {
        var account = provider.GetAs<DatabaseAccountResource>(subscriptionIdentifier, resourceGroupIdentifier, accountName);
        if (account == null)
        {
            return new ControlPlaneOperationResult<SqlContainerResource[]>(
                OperationResult.NotFound, null,
                string.Format(DatabaseAccountNotFoundMessageTemplate, accountName),
                DatabaseAccountNotFoundCode);
        }

        var parentId = SqlContainerParentId(accountName, databaseName);
        var containers = provider.ListSubresourcesAs<SqlContainerResource>(
            subscriptionIdentifier, resourceGroupIdentifier, parentId,
            nameof(Subresource.SqlContainers).ToLowerInvariant());

        return new ControlPlaneOperationResult<SqlContainerResource[]>(OperationResult.Success, containers, null, null);
    }

    public ControlPlaneOperationResult<SqlContainerThroughputSettingsResponse> GetSqlContainerThroughput(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName,
        string databaseName,
        string containerName)
    {
        var parentId = SqlContainerParentId(accountName, databaseName);
        var container = provider.GetSubresourceAs<SqlContainerResource>(
            subscriptionIdentifier, resourceGroupIdentifier, containerName, parentId,
            nameof(Subresource.SqlContainers).ToLowerInvariant());

        if (container == null)
        {
            return new ControlPlaneOperationResult<SqlContainerThroughputSettingsResponse>(
                OperationResult.NotFound, null,
                $"SQL container '{containerName}' could not be found under database '{databaseName}'.",
                "SqlContainerNotFound");
        }

        return new ControlPlaneOperationResult<SqlContainerThroughputSettingsResponse>(
            OperationResult.Success, SqlContainerThroughputSettingsResponse.From(container), null, null);
    }

    public ControlPlaneOperationResult<SqlContainerThroughputSettingsResponse> UpdateSqlContainerThroughput(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName,
        string databaseName,
        string containerName,
        UpdateSqlContainerThroughputRequest request)
    {
        var parentId = SqlContainerParentId(accountName, databaseName);
        var container = provider.GetSubresourceAs<SqlContainerResource>(
            subscriptionIdentifier, resourceGroupIdentifier, containerName, parentId,
            nameof(Subresource.SqlContainers).ToLowerInvariant());

        if (container == null)
        {
            return new ControlPlaneOperationResult<SqlContainerThroughputSettingsResponse>(
                OperationResult.NotFound, null,
                $"SQL container '{containerName}' could not be found under database '{databaseName}'.",
                "SqlContainerNotFound");
        }

        SqlContainerResourceProperties.UpdateThroughputFromRequest(container.Properties, request);

        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, containerName,
            parentId, nameof(Subresource.SqlContainers).ToLowerInvariant(), container);

        return new ControlPlaneOperationResult<SqlContainerThroughputSettingsResponse>(
            OperationResult.Success, SqlContainerThroughputSettingsResponse.From(container), null, null);
    }

    public DatabaseAccountNameAvailabilityResponse CheckNameAvailability(string accountName)
    {
        var dnsEntry = GlobalDnsEntries.GetEntry(CosmosDbService.UniqueName, accountName);
        if (dnsEntry == null)
        {
            return new DatabaseAccountNameAvailabilityResponse { NameAvailable = true };
        }

        return new DatabaseAccountNameAvailabilityResponse
        {
            NameAvailable = false,
            Reason = "AlreadyExists",
            Message = $"The database account name '{accountName}' is already in use."
        };
    }

    public ControlPlaneOperationResult RegenerateKey(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName,
        string keyKind)
    {
        var resource = provider.GetAs<DatabaseAccountResource>(subscriptionIdentifier, resourceGroupIdentifier, accountName);

        if (resource == null)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                string.Format(DatabaseAccountNotFoundMessageTemplate, accountName),
                DatabaseAccountNotFoundCode);
        }

        var props = resource.Properties;
        var newKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        switch (keyKind.ToLowerInvariant())
        {
            case "primary":
                props.PrimaryMasterKey = newKey;
                break;
            case "secondary":
                props.SecondaryMasterKey = newKey;
                break;
            case "primaryreadonly":
                props.PrimaryReadonlyMasterKey = newKey;
                break;
            case "secondaryreadonly":
                props.SecondaryReadonlyMasterKey = newKey;
                break;
            default:
                return new ControlPlaneOperationResult(OperationResult.Failed, $"Unknown keyKind '{keyKind}'.", "InvalidKeyKind");
        }

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, accountName, resource);

        return new ControlPlaneOperationResult(OperationResult.Success);
    }

    public ControlPlaneOperationResult<DatabaseAccountConnectionStringsResponse> GetConnectionStrings(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName)
    {
        var resource = provider.GetAs<DatabaseAccountResource>(subscriptionIdentifier, resourceGroupIdentifier, accountName);

        if (resource == null)
        {
            return new ControlPlaneOperationResult<DatabaseAccountConnectionStringsResponse>(
                OperationResult.NotFound,
                null,
                string.Format(DatabaseAccountNotFoundMessageTemplate, accountName),
                DatabaseAccountNotFoundCode);
        }

        var props = resource.Properties;
        var endpoint = props?.DocumentEndpoint ?? string.Empty;
        var primaryKey = props?.PrimaryMasterKey ?? string.Empty;
        var secondaryKey = props?.SecondaryMasterKey ?? string.Empty;
        var primaryRoKey = props?.PrimaryReadonlyMasterKey ?? string.Empty;
        var secondaryRoKey = props?.SecondaryReadonlyMasterKey ?? string.Empty;

        var result = DatabaseAccountConnectionStringsResponse.FromKeys(endpoint, primaryKey, secondaryKey, primaryRoKey, secondaryRoKey);

        return new ControlPlaneOperationResult<DatabaseAccountConnectionStringsResponse>(OperationResult.Success, result, null, null);
    }
}
