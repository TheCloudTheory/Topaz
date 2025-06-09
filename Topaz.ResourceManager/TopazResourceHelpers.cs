using Topaz.Shared;

namespace Topaz.ResourceManager;

public static class TopazResourceHelpers
{
    public static Uri GetKeyVaultEndpoint(string vaultName) => new($"https://localhost:{GlobalSettings.DefaultKeyVaultPort}/{vaultName}");
}