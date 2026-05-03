using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry;

internal sealed class ContainerRegistryResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<ContainerRegistryService>(logger);
