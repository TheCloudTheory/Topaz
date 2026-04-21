using Topaz.Service.Shared;
using Topaz.Service.Storage.Endpoints.Table;
using Topaz.Shared;

namespace Topaz.Service.Storage.Services;

public sealed class TableStorageService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => AzureStorageService.LocalDirectoryPath;
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "tablestorage";

    public string Name => "Table Storage";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        // OPTIONS preflight must be first so it takes priority over all other routes
        new PreflightTableRequestEndpoint(logger),
        // Specific routes first so they take priority over wildcard routes
        new GetTableServicePropertiesEndpoint(logger),
        new SetTableServicePropertiesEndpoint(logger),
        new ListTablesEndpoint(logger),
        new CreateTableEndpoint(logger),
        new GetTableEndpoint(logger),
        new DeleteTableEndpoint(logger),
        // Regex entity-key routes before wildcard routes for the same method
        new GetTableEntityEndpoint(logger),
        new InsertOrMergeTableEntityEndpoint(logger),
        new PutTableEntityEndpoint(logger),
        new PatchTableEntityEndpoint(logger),
        new DeleteTableEntityEndpoint(logger),
        // Wildcard routes last
        new QueryTableEntitiesEndpoint(logger),
        new InsertTableEntityEndpoint(logger),
        new SetTableAclEndpoint(logger),
    ];

    public void Bootstrap()
    {
    }
}
