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
        // Specific routes first so they take priority over wildcard routes
        new GetTableServicePropertiesEndpoint(logger),
        new ListTablesEndpoint(logger),
        new CreateTableEndpoint(logger),
        new DeleteTableEndpoint(logger),
        // Regex entity-key routes before wildcard routes for the same method
        new InsertOrMergeTableEntityEndpoint(logger),
        new PutTableEntityEndpoint(logger),
        new PatchTableEntityEndpoint(logger),
        // Wildcard routes last
        new QueryTableEntitiesEndpoint(logger),
        new InsertTableEntityEndpoint(logger),
        new SetTableAclEndpoint(logger),
    ];

    public void Bootstrap()
    {
    }
}
