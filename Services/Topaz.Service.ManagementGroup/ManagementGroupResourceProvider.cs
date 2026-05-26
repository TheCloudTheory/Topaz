using System.Text.Json;
using Topaz.Service.ManagementGroup.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup;

public sealed class ManagementGroupResourceProvider : ResourceProviderBase<ManagementGroupService>
{
    private readonly ITopazLogger _logger;

    public ManagementGroupResourceProvider(ITopazLogger logger) : base(logger)
    {
        _logger = logger;
    }

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

    internal Models.ManagementGroup? GetManagementGroup(string groupId)
    {
        var id = ValidateGroupId(groupId);
        var path = ResolveMetadataPath(id);
        if (!File.Exists(path)) return null;

        var content = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Models.ManagementGroup>(content, GlobalSettings.JsonOptions);
    }

    internal void SaveManagementGroup(string groupId, Models.ManagementGroup model)
    {
        var id = ValidateGroupId(groupId);
        var dir = Path.Combine(BasePath, id);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "metadata.json"),
            JsonSerializer.Serialize(model, GlobalSettings.JsonOptions));
    }

    internal IEnumerable<Models.ManagementGroup> ListManagementGroups()
    {
        if (!Directory.Exists(BasePath)) return [];

        return Directory.EnumerateDirectories(BasePath)
            .Select(dir => Path.Combine(dir, "metadata.json"))
            .Where(File.Exists)
            .Select(path => JsonSerializer.Deserialize<Models.ManagementGroup>(
                File.ReadAllText(path), GlobalSettings.JsonOptions)!);
    }

    internal void DeleteManagementGroup(string groupId)
    {
        var id = ValidateGroupId(groupId);
        var dir = Path.Combine(BasePath, id);
        if (!Directory.Exists(dir)) return;

        Directory.Delete(dir, recursive: true);
    }

    internal ManagementGroupSubscription? GetSubscriptionAssociation(string groupId, string subscriptionId)
    {
        var id = ValidateGroupId(groupId);
        var path = ResolveSubscriptionPath(id, subscriptionId);
        if (!File.Exists(path)) return null;

        var content = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ManagementGroupSubscription>(content, GlobalSettings.JsonOptions);
    }

    internal void SaveSubscriptionAssociation(string groupId, string subscriptionId,
        ManagementGroupSubscription model)
    {
        var id = ValidateGroupId(groupId);
        var dir = Path.Combine(BasePath, id, "subscriptions");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{subscriptionId}.json"),
            JsonSerializer.Serialize(model, GlobalSettings.JsonOptions));
    }

    internal void DeleteSubscriptionAssociation(string groupId, string subscriptionId)
    {
        var id = ValidateGroupId(groupId);
        var path = ResolveSubscriptionPath(id, subscriptionId);
        if (!File.Exists(path)) return;

        File.Delete(path);
    }

    /// <summary>Removes a subscription association from every management group except the specified target group.
    /// Used when moving a subscription to a new group so it does not remain in its previous parent.</summary>
    internal void RemoveSubscriptionFromAllOtherGroups(string targetGroupId, string subscriptionId)
    {
        if (!Directory.Exists(BasePath)) return;

        var normalizedTarget = ValidateGroupId(targetGroupId);
        foreach (var groupDir in Directory.EnumerateDirectories(BasePath))
        {
            var groupName = Path.GetFileName(groupDir);
            if (string.Equals(groupName, normalizedTarget, StringComparison.OrdinalIgnoreCase)) continue;

            var path = Path.Combine(groupDir, "subscriptions", $"{subscriptionId}.json");
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    /// <summary>Returns all subscription associations for a specific management group.</summary>
    internal IEnumerable<Models.ManagementGroupSubscription> ListSubscriptionAssociationsForGroup(string groupId)
    {
        var id = ValidateGroupId(groupId);
        var subsDir = Path.Combine(BasePath, id, "subscriptions");
        if (!Directory.Exists(subsDir)) return [];

        return Directory.EnumerateFiles(subsDir, "*.json")
            .Select(file => JsonSerializer.Deserialize<ManagementGroupSubscription>(
                File.ReadAllText(file), GlobalSettings.JsonOptions)!)
            .Where(s => s != null);
    }

    /// <summary>Returns all subscription associations across every management group, deduplicated by subscription ID.</summary>
    internal IEnumerable<Models.ManagementGroupSubscription> ListAllSubscriptionAssociations()
    {
        if (!Directory.Exists(BasePath)) return [];

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<Models.ManagementGroupSubscription>();

        foreach (var groupDir in Directory.EnumerateDirectories(BasePath))
        {
            var subsDir = Path.Combine(groupDir, "subscriptions");
            if (!Directory.Exists(subsDir)) continue;

            foreach (var file in Directory.EnumerateFiles(subsDir, "*.json"))
            {
                var sub = JsonSerializer.Deserialize<Models.ManagementGroupSubscription>(
                    File.ReadAllText(file), GlobalSettings.JsonOptions);
                if (sub != null && seen.Add(sub.Name))
                    result.Add(sub);
            }
        }

        return result;
    }

    internal HierarchySettings? GetHierarchySettings(string groupId)
    {
        var id = ValidateGroupId(groupId);
        var path = ResolveHierarchySettingsPath(id);
        if (!File.Exists(path)) return null;

        return JsonSerializer.Deserialize<HierarchySettings>(File.ReadAllText(path), GlobalSettings.JsonOptions);
    }

    internal void SaveHierarchySettings(string groupId, HierarchySettings settings)
    {
        var id = ValidateGroupId(groupId);
        var dir = Path.Combine(BasePath, id);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "settings.json"),
            JsonSerializer.Serialize(settings, GlobalSettings.JsonOptions));
    }

    internal void DeleteHierarchySettings(string groupId)
    {
        var id = ValidateGroupId(groupId);
        var path = ResolveHierarchySettingsPath(id);
        if (File.Exists(path))
            File.Delete(path);
    }

    private static string ResolveHierarchySettingsPath(string groupId) =>
        Path.Combine(BasePath, groupId, "settings.json");

    /// <summary>Returns true if any persisted management group references this group as its parent.</summary>
    internal bool HasChildren(string groupId)
    {
        return ListManagementGroups().Any(mg =>
            mg.Properties.Details.Parent != null &&
            string.Equals(mg.Name, groupId, StringComparison.OrdinalIgnoreCase) is false &&
            mg.Properties.Details.Parent.Id.EndsWith($"/{groupId}", StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveSubscriptionPath(string groupId, string subscriptionId)
    {
        return Path.Combine(BasePath, groupId, "subscriptions", $"{subscriptionId}.json");
    }

    public IEnumerable<string> ListAsGenericResources()
    {
        if (!Directory.Exists(BasePath))
            return [];

        return Directory.EnumerateDirectories(BasePath)
            .Select(dir => Path.Combine(dir, "metadata.json"))
            .Where(File.Exists)
            .Select(path =>
            {
                try
                {
                    return File.ReadAllText(path);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(nameof(ManagementGroupResourceProvider), nameof(ListAsGenericResources),
                        "Failed to read management group resource from '{0}': {1}", path, ex.Message);
                    return null;
                }
            })
            .Where(json => json != null)
            .Select(json => json!);
    }

    private static string ResolveMetadataPath(string groupId)
    {
        var dir = Path.Combine(BasePath, groupId);
        if (Directory.Exists(dir)) return Path.Combine(dir, "metadata.json");
        
        // Case-insensitive fallback for case-sensitive file systems.
        var match = Directory.Exists(BasePath)
            ? Directory.EnumerateDirectories(BasePath)
                .FirstOrDefault(d => string.Equals(
                    Path.GetFileName(d), groupId, StringComparison.OrdinalIgnoreCase))
            : null;
        
        if (match != null)
            dir = match;

        return Path.Combine(dir, "metadata.json");
    }
}
