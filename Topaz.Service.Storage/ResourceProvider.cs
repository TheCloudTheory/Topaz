using Topaz.Service.Shared;
using Topaz.Service.Storage.Services;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class ResourceProvider(ITopazLogger logger) : ResourceProviderBase<AzureStorageService>(logger)
{
    internal bool CheckIfStorageAccountExists(string name)
    {
        var accountPath = Path.Combine(".topaz", AzureStorageService.LocalDirectoryPath, name);
        return Directory.Exists(accountPath);
    }
}
