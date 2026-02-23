using Topaz.Service.Entra.Endpoints;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra;

public class EntraService(ITopazLogger logger) : IServiceDefinition 
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => ".entra";
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "entra";
    public string Name => "Entra ID";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new EntraUserGraphEndpoint(logger),
        new EntraServicePrincipalGraphEndpoint()
    ];

    public void Bootstrap()
    {
    }
}