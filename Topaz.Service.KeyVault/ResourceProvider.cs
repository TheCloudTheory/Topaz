using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

internal sealed class ResourceProvider(ILogger logger) : ResourceProviderBase<KeyVaultService>(logger)
{
    public string GetKeyVaultPath(string keyVaultName)
    {
        return Path.Combine(BaseEmulatorPath, KeyVaultService.LocalDirectoryPath, keyVaultName, "data");
    }
}
