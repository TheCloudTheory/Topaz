using Topaz.Shared;

namespace Topaz.ResourceManager;

public static class TopazResourceHelpers
{
    public static Uri GetKeyVaultEndpoint(string vaultName) => new Uri($"https://localhost:{GlobalSettings.DefaultKeyVaultPort}/{vaultName}");
}