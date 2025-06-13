using Topaz.Service.Shared;
using Topaz.Service.Storage.Endpoints;
using Topaz.Shared;

namespace Topaz.Service.Storage.Services;

public class BlobStorageService(ILogger logger) : IServiceDefinition
{
    public string Name => "Blob Storage";
    public static string LocalDirectoryPath => ".blob";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new BlobEndpoint(logger)
    ];
}