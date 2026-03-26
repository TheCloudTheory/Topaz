using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

internal sealed class KeyVaultResourceProvider(ITopazLogger logger) : ResourceProviderBase<KeyVaultService>(logger)
{
}
