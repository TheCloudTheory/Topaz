using System.Text.Json;
using Azure.Local.Service.Shared;
using Azure.Local.Shared;

namespace Azure.Local.Service.KeyVault;

internal sealed class ResourceProvider(ILogger logger) : ResourceProviderBase<KeyVaultService>(logger)
{
    private readonly ILogger logger = logger;

    internal Models.KeyVault Create(string name, string resourceGroup, string location)
    {
        var keyVaultPath = GetKeyVaultPath(name);
        if (File.Exists(keyVaultPath))
        {
            this.logger.LogDebug($"The resource group '{name}' already exists, no changes applied.");

            var content = File.ReadAllText(keyVaultPath);
            var data = JsonSerializer.Deserialize<Models.KeyVault>(content);

            return data!;
        }

        var newData = new Models.KeyVault(name, resourceGroup, location);

        File.WriteAllText(keyVaultPath, JsonSerializer.Serialize(newData, GlobalSettings.JsonOptions));

        return newData;
    }

    internal string GetKeyVaultPath(string name)
    {
        var fileName = $"{name}.json";
        var keyVaultPath = Path.Combine(KeyVaultService.LocalDirectoryPath, fileName);

        return keyVaultPath;
    }
}
