using Topaz.EventPipeline;
using Topaz.Service.CosmosDb.Endpoints.DatabaseAccounts;
using Topaz.Service.CosmosDb.Endpoints.DataPlane;
using Topaz.Service.CosmosDb.Endpoints.DataPlane.Collections;
using Topaz.Service.CosmosDb.Endpoints.DataPlane.Databases;
using Topaz.Service.CosmosDb.Endpoints.DataPlane.Documents;
using Topaz.Service.CosmosDb.Endpoints.SqlContainers;
using Topaz.Service.CosmosDb.Endpoints.SqlDatabases;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb;

public sealed class CosmosDbService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-cosmos-db");
    public static IReadOnlyCollection<string>? Subresources => ["sqldatabases", "sqlcontainers"];
    public static string UniqueName => "cosmos-db";

    public string Name => "Azure Cosmos DB";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateDatabaseAccountEndpoint(eventPipeline, logger),
        new GetDatabaseAccountEndpoint(eventPipeline, logger),
        new DeleteDatabaseAccountEndpoint(eventPipeline, logger),
        new UpdateDatabaseAccountEndpoint(eventPipeline, logger),
        new ListDatabaseAccountsByResourceGroupEndpoint(eventPipeline, logger),
        new ListDatabaseAccountsBySubscriptionEndpoint(eventPipeline, logger),
        new ListKeysDatabaseAccountEndpoint(eventPipeline, logger),
        new ListReadOnlyKeysDatabaseAccountEndpoint(eventPipeline, logger),
        new ListConnectionStringsDatabaseAccountEndpoint(eventPipeline, logger),
        new RegenerateKeyDatabaseAccountEndpoint(eventPipeline, logger),
        new CheckNameAvailabilityEndpoint(eventPipeline, logger),
        new CreateOrUpdateSqlDatabaseEndpoint(eventPipeline, logger),
        new GetSqlDatabaseEndpoint(eventPipeline, logger),
        new DeleteSqlDatabaseEndpoint(eventPipeline, logger),
        new ListSqlDatabasesEndpoint(eventPipeline, logger),
        new GetSqlDatabaseThroughputEndpoint(eventPipeline, logger),
        new UpdateSqlDatabaseThroughputEndpoint(eventPipeline, logger),
        new CreateOrUpdateSqlContainerEndpoint(eventPipeline, logger),
        new GetSqlContainerEndpoint(eventPipeline, logger),
        new DeleteSqlContainerEndpoint(eventPipeline, logger),
        new ListSqlContainersEndpoint(eventPipeline, logger),
        new GetSqlContainerThroughputEndpoint(eventPipeline, logger),
        new UpdateSqlContainerThroughputEndpoint(eventPipeline, logger),
        new CreateDatabaseEndpoint(eventPipeline, logger),
        new GetDatabaseEndpoint(eventPipeline, logger),
        new DeleteDatabaseEndpoint(eventPipeline, logger),
        new ListDatabasesEndpoint(eventPipeline, logger),
        new GetAccountPropertiesEndpoint(eventPipeline, logger),
        new CreateCollectionEndpoint(eventPipeline, logger),
        new GetCollectionEndpoint(eventPipeline, logger),
        new DeleteCollectionEndpoint(eventPipeline, logger),
        new ReplaceCollectionEndpoint(eventPipeline, logger),
        new ListCollectionsEndpoint(eventPipeline, logger),
        new GetPartitionKeyRangesEndpoint(eventPipeline, logger),
        new CreateDocumentEndpoint(eventPipeline, logger),
        new GetDocumentEndpoint(eventPipeline, logger),
        new ReplaceDocumentEndpoint(eventPipeline, logger),
        new PatchDocumentEndpoint(eventPipeline, logger),
        new DeleteDocumentEndpoint(eventPipeline, logger),
        new ListDocumentsEndpoint(eventPipeline, logger)
    ];

    public void Bootstrap() { }
}
