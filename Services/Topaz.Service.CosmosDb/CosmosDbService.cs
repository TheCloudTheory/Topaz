using Topaz.EventPipeline;
using Topaz.Service.CosmosDb.DataPlane;
using Topaz.Service.CosmosDb.DataPlane.Databases;
using Topaz.Service.CosmosDb.Endpoints;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb;

public sealed class CosmosDbService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    // eventPipeline and logger are retained for use when endpoints are registered in the next implementation phase.
    private readonly Pipeline _eventPipeline = eventPipeline;
    private readonly ITopazLogger _logger = logger;

    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-cosmos-db");
    public static IReadOnlyCollection<string>? Subresources => ["sqldatabases", "sqlcontainers"];
    public static string UniqueName => "cosmos-db";

    public string Name => "Azure Cosmos DB";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateDatabaseAccountEndpoint(_eventPipeline, _logger),
        new GetDatabaseAccountEndpoint(_eventPipeline, _logger),
        new DeleteDatabaseAccountEndpoint(_eventPipeline, _logger),
        new UpdateDatabaseAccountEndpoint(_eventPipeline, _logger),
        new ListDatabaseAccountsByResourceGroupEndpoint(_eventPipeline, _logger),
        new ListDatabaseAccountsBySubscriptionEndpoint(_eventPipeline, _logger),
        new ListKeysDatabaseAccountEndpoint(_eventPipeline, _logger),
        new ListReadOnlyKeysDatabaseAccountEndpoint(_eventPipeline, _logger),
        new ListConnectionStringsDatabaseAccountEndpoint(_eventPipeline, _logger),
        new RegenerateKeyDatabaseAccountEndpoint(_eventPipeline, _logger),
        new CheckNameAvailabilityEndpoint(_eventPipeline, _logger),
        new CreateOrUpdateSqlDatabaseEndpoint(_eventPipeline, _logger),
        new GetSqlDatabaseEndpoint(_eventPipeline, _logger),
        new DeleteSqlDatabaseEndpoint(_eventPipeline, _logger),
        new ListSqlDatabasesEndpoint(_eventPipeline, _logger),
        new GetSqlDatabaseThroughputEndpoint(_eventPipeline, _logger),
        new UpdateSqlDatabaseThroughputEndpoint(_eventPipeline, _logger),
        new CreateOrUpdateSqlContainerEndpoint(_eventPipeline, _logger),
        new GetSqlContainerEndpoint(_eventPipeline, _logger),
        new DeleteSqlContainerEndpoint(_eventPipeline, _logger),
        new ListSqlContainersEndpoint(_eventPipeline, _logger),
        new GetSqlContainerThroughputEndpoint(_eventPipeline, _logger),
        new UpdateSqlContainerThroughputEndpoint(_eventPipeline, _logger),
        new CreateDatabaseEndpoint(_eventPipeline, _logger),
        new GetDatabaseEndpoint(_eventPipeline, _logger),
        new DeleteDatabaseEndpoint(_eventPipeline, _logger),
        new ListDatabasesEndpoint(_eventPipeline, _logger),
        new GetAccountPropertiesEndpoint(_eventPipeline, _logger)
    ];

    public void Bootstrap() { }
}
