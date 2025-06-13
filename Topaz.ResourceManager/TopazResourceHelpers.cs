using Topaz.Shared;

namespace Topaz.ResourceManager;

public static class TopazResourceHelpers
{
    public static Uri GetKeyVaultEndpoint(string vaultName) => new($"https://localhost:{GlobalSettings.DefaultKeyVaultPort}/{vaultName}");
    public static string GetAzureStorageConnectionString(string storageAccountName) =>
        $"DefaultEndpointsProtocol=http;AccountName={storageAccountName};AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:{GlobalSettings.DefaultBlobStoragePort}/{storageAccountName};QueueEndpoint=http://localhost:8899;TableEndpoint=http://localhost:{GlobalSettings.DefaultTableStoragePort}/storage/{storageAccountName};";
}