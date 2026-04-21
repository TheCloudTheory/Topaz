using System.Text.Json;
using Topaz.Service.ManagementGroup.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup;

internal sealed class ManagementGroupResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<ManagementGroupService>(logger)
{
    private static string BasePath =>
        Path.Combine(GlobalSettings.MainEmulatorDirectory, ".management-group");

    /// <summary>
    /// Validates that a management group ID is a safe, non-empty directory name
    /// (no path separators or traversal sequences).
    /// </summary>
    private static string ValidateGroupId(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            throw new ArgumentException("Management group ID must not be empty.", nameof(groupId));
        if (groupId.Contains('/') || groupId.Contains('\\') || groupId.Contains(".."))
            throw new ArgumentException($"Management group ID '{groupId}' contains invalid characters.",
                nameof(groupId));
        return groupId;
    }

    public Models.ManagementGroup? GetManagementGroup(string groupId)
    {
        var id = ValidateGroupId(groupId);
        var path = ResolveMetadataPath(id);
        if (!File.Exists(path)) return null;

        var content = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Models.ManagementGroup>(content, GlobalSettings.JsonOptions);
    }

    public void SaveManagementGroup(string groupId, Models.ManagementGroup model)
    {
        var id = ValidateGroupId(groupId);
        var dir = Path.Combine(BasePath, id);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "metadata.json"),
            JsonSerializer.Serialize(model, GlobalSettings.JsonOptions));
    }

    public IEnumerable<Models.ManagementGroup> ListManagementGroups()
    {
        if (!Directory.Exists(BasePath)) return [];

        return Directory.EnumerateDirectories(BasePath)
            .Select(dir => Path.Combine(dir, "metadata.json"))
            .Where(File.Exists)
            .Select(path => JsonSerializer.Deserialize<Models.ManagementGroup>(
                File.ReadAllText(path), GlobalSettings.JsonOptions)!)
            .Where(mg => mg != null);
    }

    public bool DeleteManagementGroup(string groupId)
    {
        var id = ValidateGroupId(groupId);
        var dir = Path.Combine(BasePath, id);
        if (!Directory.Exists(dir)) return false;

        Directory.Delete(dir, recursive: true);
        return true;
    }

    /// <summary>Returns true if any persisted management group references this group as its parent.</summary>
    public bool HasChildren(string groupId)
    {
        return ListManagementGroups().Any(mg =>
            mg.Properties.Details.Parent != null &&
            string.Equals(mg.Name, groupId, StringComparison.OrdinalIgnoreCase) is false &&
            mg.Properties.Details.Parent.Id.EndsWith($"/{groupId}", StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveMetadataPath(string groupId)
    {
        var dir = Path.Combine(BasePath, groupId);
        if (!Directory.Exists(dir))
        {
            // Case-insensitive fallback for case-sensitive file systems.
            var match = Directory.Exists(BasePath)
                ? Directory.EnumerateDirectories(BasePath)
                    .FirstOrDefault(d => string.Equals(
                        Path.GetFileName(d), groupId, StringComparison.OrdinalIgnoreCase))
                : null;
            if (match != null)
                dir = match;
        }

        return Path.Combine(dir, "metadata.json");
    }
}
