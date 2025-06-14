using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup;

public sealed class ResourceGroupService(ITopazLogger logger) : IServiceDefinition
{
    public static string LocalDirectoryPath => ".resource-groups";
    private readonly ITopazLogger _topazLogger = logger;

    public string Name => "Resource Group";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new ResourceGroupEndpoint(new ResourceProvider(this._topazLogger), this._topazLogger)
    ];
}
