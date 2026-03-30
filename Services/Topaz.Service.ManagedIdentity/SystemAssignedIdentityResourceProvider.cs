using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagedIdentity;

internal sealed class SystemAssignedIdentityResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<ManagedIdentityService>(logger);
