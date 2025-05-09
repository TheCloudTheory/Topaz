using Azure.Local.Service.Shared;
using Azure.Local.Shared;

namespace Azure.Local.Service.ResourceGroup;

public sealed class ResourceGroupService(ILogger logger) : IServiceDefinition
{
    public static string LocalDirectoryPath => ".resource-groups";
    private readonly ILogger logger = logger;

    public string Name => "Resource Group";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new ResourceGroupEndpoint(this.logger)
    ];
}
