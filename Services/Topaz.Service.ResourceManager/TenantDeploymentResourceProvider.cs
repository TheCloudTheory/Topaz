using System.Text.Json;
using Topaz.Service.ResourceManager.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

internal sealed class TenantDeploymentResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<TenantDeploymentService>(logger)
{
    private const string TenantDir = ".tenant";
    private const string DeploymentsSubDir = ".resource-manager";

    public IEnumerable<TenantDeploymentResource> ListDeployments()
    {
        var deploymentsDir = Path.Combine(GlobalSettings.MainEmulatorDirectory, TenantDir, DeploymentsSubDir);
        if (!Directory.Exists(deploymentsDir)) yield break;

        // Enumerate exactly one level deep: {deploymentsDir}/{deploymentName}/metadata.json
        foreach (var deploymentDir in Directory.EnumerateDirectories(deploymentsDir))
        {
            var metadataFile = Path.Combine(deploymentDir, "metadata.json");
            if (!File.Exists(metadataFile)) continue;

            var content = File.ReadAllText(metadataFile);
            if (string.IsNullOrWhiteSpace(content)) continue;

            var resource = JsonSerializer.Deserialize<TenantDeploymentResource>(
                content, GlobalSettings.JsonOptions);
            if (resource != null) yield return resource;
        }
    }
}
