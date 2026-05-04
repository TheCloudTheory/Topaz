using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Endpoints.Table;
using Topaz.Shared;

namespace Topaz.Service.Storage.Services;

public sealed class TableStorageService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => AzureStorageService.LocalDirectoryPath;
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "tablestorage";

    public string Name => "Table Storage";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        // OPTIONS preflight must be first so it takes priority over all other routes
        new PreflightTableRequestEndpoint(eventPipeline, logger),
        // Specific routes first so they take priority over wildcard routes
        new GetTableServicePropertiesEndpoint(eventPipeline, logger),
        new SetTableServicePropertiesEndpoint(eventPipeline, logger),
        new ListTablesEndpoint(eventPipeline, logger),
        new CreateTableEndpoint(eventPipeline, logger),
        new GetTableEndpoint(eventPipeline, logger),
        new DeleteTableEndpoint(eventPipeline, logger),
        // Regex entity-key routes before wildcard routes for the same method
        new GetTableEntityEndpoint(eventPipeline, logger),
        new InsertOrMergeTableEntityEndpoint(eventPipeline, logger),
        new PutTableEntityEndpoint(eventPipeline, logger),
        new PatchTableEntityEndpoint(eventPipeline, logger),
        new DeleteTableEntityEndpoint(eventPipeline, logger),
        // Wildcard routes last
        new QueryTableEntitiesEndpoint(eventPipeline, logger),
        new InsertTableEntityEndpoint(eventPipeline, logger),
        new SetTableAclEndpoint(eventPipeline, logger),
    ];

    public void Bootstrap()
    {
    }
}
