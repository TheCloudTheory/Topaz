using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.VirtualMachine;

internal sealed class AvailabilitySetResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<AvailabilitySetService>(logger);