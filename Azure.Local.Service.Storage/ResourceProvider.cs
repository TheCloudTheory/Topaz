using Azure.Local.Service.Shared;
using Azure.Local.Shared;

namespace Azure.Local.Service.Storage;

internal sealed class ResourceProvider(ILogger logger) : ResourceProviderBase<AzureStorageService>(logger)
{
    internal bool CheckIfStorageAccountExists(string name)
    {
        var accountPath = Path.Combine(".abazure", AzureStorageService.LocalDirectoryPath, name);
        return Directory.Exists(accountPath);
    }
}
