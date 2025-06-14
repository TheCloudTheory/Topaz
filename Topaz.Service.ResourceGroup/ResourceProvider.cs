using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup;

public sealed class ResourceProvider(ITopazLogger logger) : ResourceProviderBase<ResourceGroupService>(logger)
{
}
