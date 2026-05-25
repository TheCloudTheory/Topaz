using System.Text.Json;
using Topaz.Service.ResourceManager.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

public sealed class TenantDeploymentResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<TenantDeploymentService>(logger)
{
    private const string TenantDir = ".tenant";
    private const string DeploymentsSubDir = ".resource-manager";

    private string DeploymentDir(string name) =>
        Path.Combine(GlobalSettings.MainEmulatorDirectory, TenantDir, DeploymentsSubDir, name);

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

    public void CreateOrUpdateDeployment(string name, TenantDeploymentResource resource)
    {
        var dir = DeploymentDir(name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "metadata.json"),
            JsonSerializer.Serialize(resource, GlobalSettings.JsonOptions));
    }

    public TenantDeploymentResource? GetDeployment(string name)
    {
        var metadataFile = Path.Combine(DeploymentDir(name), "metadata.json");
        if (!File.Exists(metadataFile)) return null;
        var content = File.ReadAllText(metadataFile);
        return string.IsNullOrWhiteSpace(content)
            ? null
            : JsonSerializer.Deserialize<TenantDeploymentResource>(content, GlobalSettings.JsonOptions);
    }

    public void DeleteDeployment(string name)
    {
        var dir = DeploymentDir(name);
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }
}
