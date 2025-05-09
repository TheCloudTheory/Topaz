using Azure.Local.Service.Shared;
using Azure.Local.Shared;

namespace Azure.Local.Service.ResourceGroup;

public sealed class ResourceProvider(ILogger logger) : ResourceProviderBase<ResourceGroupService>(logger)
{
}
