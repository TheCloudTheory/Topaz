using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

public sealed class ResourceManagerResourceProvider(ITopazLogger logger) : ResourceProviderBase<ResourceManagerService>(logger)
{
}