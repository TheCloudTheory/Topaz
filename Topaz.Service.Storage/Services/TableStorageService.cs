using Topaz.Service.Shared;
using Topaz.Service.Storage.Endpoints;
using Topaz.Shared;

namespace Topaz.Service.Storage.Services;

public sealed class TableStorageService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(AzureStorageService.LocalDirectoryPath, "{storageAccount}", ".table");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "tablestorage";

    public string Name => "Table Storage";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new TableEndpoint(logger),
    ];
}
