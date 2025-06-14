using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

internal sealed class ResourceProvider(ITopazLogger logger) : ResourceProviderBase<KeyVaultService>(logger)
{
    public string GetKeyVaultPath(string keyVaultName)
    {
        return Path.Combine(BaseEmulatorPath, KeyVaultService.LocalDirectoryPath, keyVaultName, "data");
    }
}
