using System.Text.Json;
using System.Text.Json.Nodes;
using Topaz.Service.ResourceManager.Models.Responses;

namespace Topaz.Service.ResourceManager.Deployment;

/// <summary>
/// Computes a property-level diff between two JSON representations of an ARM resource.
/// Top-level identity fields (id, name, type) are excluded from the delta.
/// Arrays are treated atomically — individual element diffs are not computed.
/// </summary>
internal static class WhatIfEngine
{
    // Properties that exist in an ARM template resource definition but are not real resource
    // state: id/name/type are identity fields; apiVersion and dependsOn are deployment-time only.
    private static readonly HashSet<string> TopLevelExclusions = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "name", "type", "apiVersion", "dependsOn", "comments", "condition", "copy"
    };

    /// <summary>
    /// Produces a list of property changes between <paramref name="before"/> and <paramref name="after"/>.
    /// Both arguments may be null (representing absent resources), in which case no delta is returned.
    /// </summary>
    public static List<WhatIfPropertyChange> ComputeDelta(JsonNode? before, JsonNode? after)
    {
        var changes = new List<WhatIfPropertyChange>();
        DiffNodes(before, after, string.Empty, changes, isTopLevel: true);
        return changes;
    }

    private static void DiffNodes(
        JsonNode? before,
        JsonNode? after,
        string path,
        List<WhatIfPropertyChange> changes,
        bool isTopLevel = false)
    {
        if (before is null && after is null)
            return;

        if (before is null)
        {
            changes.Add(WhatIfPropertyChange.Create(path, WhatIfPropertyChangeType.Create, null,
                after!.DeepClone()));
            return;
        }

        if (after is null)
        {
            changes.Add(WhatIfPropertyChange.Create(path, WhatIfPropertyChangeType.Delete,
                before.DeepClone(), null));
            return;
        }

        // Both present — compare by kind
        if (before is JsonObject beforeObj && after is JsonObject afterObj)
        {
            DiffObjects(beforeObj, afterObj, path, changes, isTopLevel);
        }
        else if (before is JsonArray beforeArr && after is JsonArray afterArr)
        {
            // Arrays are treated atomically
            var beforeJson = beforeArr.ToJsonString();
            var afterJson = afterArr.ToJsonString();
            if (beforeJson != afterJson)
            {
                changes.Add(WhatIfPropertyChange.Create(path, WhatIfPropertyChangeType.Array,
                    before.DeepClone(), after.DeepClone()));
            }
        }
        else
        {
            // Primitive or type mismatch
            var beforeJson = before.ToJsonString();
            var afterJson = after.ToJsonString();
            if (beforeJson != afterJson)
            {
                changes.Add(WhatIfPropertyChange.Create(path, WhatIfPropertyChangeType.Modify,
                    before.DeepClone(), after.DeepClone()));
            }
        }
    }

    private static void DiffObjects(
        JsonObject before,
        JsonObject after,
        string path,
        List<WhatIfPropertyChange> changes,
        bool isTopLevel)
    {
        // Template-first traversal: only examine keys present in "after" (the template).
        // Keys in "before" that are absent from "after" are not flagged as deletions because
        // ARM Incremental-mode deployments preserve properties that the template does not mention.
        foreach (var (key, afterChild) in after)
        {
            if (isTopLevel && TopLevelExclusions.Contains(key))
                continue;

            var childPath = string.IsNullOrEmpty(path) ? key : $"{path}.{key}";

            var hasBefore = before.TryGetPropertyValue(key, out var beforeChild);

            if (!hasBefore)
            {
                changes.Add(WhatIfPropertyChange.Create(childPath, WhatIfPropertyChangeType.Create,
                    null, afterChild?.DeepClone()));
            }
            else
            {
                DiffNodes(beforeChild, afterChild, childPath, changes);
            }
        }
    }
}
