using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ContainerInstances;

internal sealed class ContainerInstancesResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<ContainerInstancesService>(logger);