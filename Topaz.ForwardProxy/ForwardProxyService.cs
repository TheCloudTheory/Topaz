using Topaz.Service.Shared;

namespace Topaz.ForwardProxy;

public sealed class ForwardProxyService : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => string.Empty;
    public static IReadOnlyCollection<string>? Subresources => [];
    public static string UniqueName => "forward-proxy";
    public string Name => "ForwardProxy";
    public bool IsTopazService => true;
    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new ForwardProxyEndpoint()
    ];
}