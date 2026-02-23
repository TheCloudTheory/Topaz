using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra;

internal sealed class EntraResourceProvider(ITopazLogger logger) : ResourceProviderBase<EntraService>(logger)
{
    private const string UsersDirectoryName = "users";
    
    public string GetServiceInstanceUsersDataPath()
    {
        return Path.Combine(BaseEmulatorPath, EntraService.LocalDirectoryPath, UsersDirectoryName, "data");
    }
}