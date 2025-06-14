using Topaz.Service.Shared;
using Topaz.Service.Storage.Endpoints;
using Topaz.Shared;

namespace Topaz.Service.Storage.Services;

public sealed class AzureStorageService(ITopazLogger logger) : IServiceDefinition
{
    public static string LocalDirectoryPath => ".azure-storage";

    public string Name => "Azure Storage";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new AzureStorageEndpoint(logger)
    ];
}
