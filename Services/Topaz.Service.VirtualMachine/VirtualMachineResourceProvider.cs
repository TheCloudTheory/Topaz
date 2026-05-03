using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.VirtualMachine;

internal sealed class VirtualMachineResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<VirtualMachineService>(logger);
