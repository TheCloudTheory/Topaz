using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup;

public sealed class ResourceGroupService(ILogger logger) : IServiceDefinition
{
    public static string LocalDirectoryPath => ".resource-groups";
    private readonly ILogger logger = logger;

    public string Name => "Resource Group";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new ResourceGroupEndpoint(new ResourceProvider(this.logger), this.logger)
    ];
}
