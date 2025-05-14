using Topaz.Service.Shared;
using Topaz.Service.Storage.Endpoints;
using Topaz.Shared;

namespace Topaz.Service.Storage.Services;

public sealed class TableStorageService(ILogger logger) : IServiceDefinition
{
    private readonly ILogger logger = logger;

    public static string LocalDirectoryPath => ".table";

    public string Name => "Table Storage";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new TableEndpoint(this.logger),
    ];
}
