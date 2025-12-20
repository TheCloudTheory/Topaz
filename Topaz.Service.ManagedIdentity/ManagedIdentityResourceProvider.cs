using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagedIdentity;

public sealed class ManagedIdentityResourceProvider(ITopazLogger logger) : ResourceProviderBase<ManagedIdentityService>(logger);