using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Disk;

internal sealed class DiskResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<DiskService>(logger);
