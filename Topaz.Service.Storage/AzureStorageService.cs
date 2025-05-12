using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage;

public sealed class AzureStorageService(ILogger logger) : IServiceDefinition
{
    public static string LocalDirectoryPath => ".azure-storage";
    private readonly ILogger logger = logger;

    public string Name => "Azure Storage";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new BlobEndpoint()
    ];
}
