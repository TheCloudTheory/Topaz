using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup;

public sealed class ResourceGroupResourceProvider(ITopazLogger logger) : ResourceProviderBase<ResourceGroupService>(logger)
{
}
