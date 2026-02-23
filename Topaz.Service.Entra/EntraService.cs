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
        new EntraServicePrincipalGraphEndpoint(logger)
    ];

    public void Bootstrap()
    {
        logger.LogDebug(nameof(EntraService), nameof(Bootstrap),$"Attempting to initialize Entra service directory directory...");
        
        var servicePath = Path.Combine(GlobalSettings.MainEmulatorDirectory, LocalDirectoryPath);
        var userPath = Path.Combine(servicePath, EntraResourceProvider.UsersDirectoryName);
        var servicePrincipalPath = Path.Combine(servicePath, EntraResourceProvider.ServicePrincipalsDirectoryName);
        var userDataPath = Path.Combine(userPath, "data");
        var servicePrincipalDataPath = Path.Combine(servicePrincipalPath, "data");
        
        CreateServiceDirectory(servicePath);
        CreateServiceDirectory(userPath);
        CreateServiceDirectory(userDataPath);
        CreateServiceDirectory(servicePrincipalPath);
        CreateServiceDirectory(servicePrincipalDataPath);
    }

    private void CreateServiceDirectory(string servicePath)
    {
        if(!Directory.Exists(servicePath))
        {
            Directory.CreateDirectory(servicePath);
            logger.LogDebug(nameof(EntraService), nameof(Bootstrap),$"Directory {servicePath} created.");
        }
        else
        {
            logger.LogDebug(nameof(EntraService), nameof(Bootstrap),$"Attempting to create {servicePath} directory - skipped.");
        }
    }
}