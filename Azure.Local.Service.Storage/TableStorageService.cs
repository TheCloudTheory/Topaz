using Azure.Local.Service.Shared;
using Azure.Local.Shared;

namespace Azure.Local.Service.Storage;

public sealed class TableStorageService(ILogger logger) : IServiceDefinition
{
    private readonly ILogger logger = logger;

    public static string LocalDirectoryPath => Path.Combine(AzureStorageService.LocalDirectoryPath, ".table");

    public string Name => "Table Storage";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new TableEndpoint(this.logger),
    ];
}
