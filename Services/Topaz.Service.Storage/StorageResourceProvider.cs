using Topaz.Service.Shared;
using Topaz.Service.Storage.Services;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class StorageResourceProvider(ITopazLogger logger) : ResourceProviderBase<AzureStorageService>(logger)
{
}
