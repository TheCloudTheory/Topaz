using Topaz.EventPipeline;
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
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "cosmos-db";

    public string Name => "Azure Cosmos DB";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateDatabaseAccountEndpoint(_eventPipeline, _logger),
        new GetDatabaseAccountEndpoint(_eventPipeline, _logger),
        new DeleteDatabaseAccountEndpoint(_eventPipeline, _logger),
        new UpdateDatabaseAccountEndpoint(_eventPipeline, _logger),
        new ListDatabaseAccountsByResourceGroupEndpoint(_eventPipeline, _logger),
        new ListDatabaseAccountsBySubscriptionEndpoint(_eventPipeline, _logger)
    ];

    public void Bootstrap() { }
}
