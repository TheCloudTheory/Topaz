using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup;

public sealed class ResourceProvider(ILogger logger) : ResourceProviderBase<ResourceGroupService>(logger)
{
}
