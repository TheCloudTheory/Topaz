using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra;

internal sealed class EntraResourceProvider(ITopazLogger logger) : ResourceProviderBase<EntraService>(logger)
{
    internal const string UsersDirectoryName = "users";
    internal const string ServicePrincipalsDirectoryName = "service-principals";
    
    public string GetServiceInstanceUsersDataPath()
    {
        return Path.Combine(BaseEmulatorPath, EntraService.LocalDirectoryPath, UsersDirectoryName, "data");
    }
    
    public string GetServiceInstanceServicePrincipalsDataPath()
    {
        return Path.Combine(BaseEmulatorPath, EntraService.LocalDirectoryPath, ServicePrincipalsDirectoryName, "data");
    }
}