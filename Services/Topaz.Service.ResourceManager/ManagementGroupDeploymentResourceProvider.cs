using System.Text.Json;
using Topaz.Service.ResourceManager.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

internal sealed class ManagementGroupDeploymentResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<ManagementGroupDeploymentService>(logger)
{
    private const string ManagementGroupDir = ".management-group";
    private const string DeploymentsSubDir = ".resource-manager";

    private static void ValidateGroupId(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId) ||
            groupId.Contains("..") ||
            groupId.Contains('/') ||
            groupId.Contains('\\'))
            throw new ArgumentException($"Invalid management group ID: '{groupId}'", nameof(groupId));
    }

    /// <summary>
    /// Resolves the on-disk path of a management group directory using a case-insensitive search.
    /// Returns null if no matching directory is found.
    /// </summary>
    private static string? ResolveManagementGroupDirectory(string groupId)
    {
        var parent = Path.Combine(GlobalSettings.MainEmulatorDirectory, ManagementGroupDir);
        if (!Directory.Exists(parent)) return null;

        return Directory.EnumerateDirectories(parent)
            .FirstOrDefault(d => string.Equals(Path.GetFileName(d), groupId, StringComparison.OrdinalIgnoreCase));
    }

    public bool ManagementGroupExists(string groupId)
    {
        ValidateGroupId(groupId);
        var mgDir = ResolveManagementGroupDirectory(groupId);
        return mgDir != null && File.Exists(Path.Combine(mgDir, "metadata.json"));
    }

    public IEnumerable<ManagementGroupDeploymentResource> ListDeployments(string groupId)
    {
        ValidateGroupId(groupId);

        var mgDir = ResolveManagementGroupDirectory(groupId);
        if (mgDir == null) yield break;

        var deploymentsDir = Path.Combine(mgDir, DeploymentsSubDir);
        if (!Directory.Exists(deploymentsDir)) yield break;

        // Enumerate exactly one level deep: {deploymentsDir}/{deploymentName}/metadata.json
        foreach (var deploymentDir in Directory.EnumerateDirectories(deploymentsDir))
        {
            var metadataFile = Path.Combine(deploymentDir, "metadata.json");
            if (!File.Exists(metadataFile)) continue;

            var content = File.ReadAllText(metadataFile);
            if (string.IsNullOrWhiteSpace(content)) continue;

            var resource = JsonSerializer.Deserialize<ManagementGroupDeploymentResource>(
                content, GlobalSettings.JsonOptions);
            if (resource != null) yield return resource;
        }
    }
}
