using Azure.Local.Service.Shared;
using Azure.Local.Shared;

namespace Azure.Local.Service.Storage;

internal sealed class TableResourceProvider(ILogger logger) : ResourceProviderBase<TableStorageService>(logger)
{
    public override IEnumerable<string> List(string id)
    {
        var servicePath = Path.Combine(BaseEmulatorPath, TableStorageService.LocalDirectoryPath, id);
        return Directory.EnumerateFiles(servicePath);
    }
}
