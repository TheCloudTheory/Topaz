using System.Text.Json;
using ACE.WhatIf;
using Topaz.Shared;

namespace Topaz.FinOps;

/// <summary>
/// Walks the .topaz emulator directory for a given subscription and returns a
/// <see cref="WhatIfChange"/> snapshot of every persisted ARM resource.
/// </summary>
internal sealed class ResourceInventoryCollector(ITopazLogger logger)
{
    public WhatIfChange[] CollectForSubscription(string subscriptionId)
    {
        var emulatorDir = GlobalSettings.MainEmulatorDirectory;
        if (!Directory.Exists(emulatorDir))
        {
            return [];
        }

        var changes = new List<WhatIfChange>();
        var jsonFiles = Directory.GetFiles(emulatorDir, "*.json", SearchOption.AllDirectories);

        foreach (var file in jsonFiles)
        {
            try
            {
                var content = File.ReadAllText(file);
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (!root.TryGetProperty("id", out var idElement) ||
                    !root.TryGetProperty("type", out var typeElement))
                {
                    continue;
                }

                var resourceId = idElement.GetString();
                var resourceType = typeElement.GetString();

                if (string.IsNullOrEmpty(resourceId) ||
                    !resourceId.StartsWith(
                        $"/subscriptions/{subscriptionId}",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var location = root.TryGetProperty("location", out var locationElement)
                    ? locationElement.GetString()
                    : null;

                changes.Add(new WhatIfChange
                {
                    resourceId = resourceId,
                    changeType = WhatIfChangeType.Create,
                    after = new WhatIfAfterBeforeChange
                    {
                        type = resourceType,
                        location = location
                    }
                });
            }
            catch (JsonException ex)
            {
                logger.LogDebug(
                    nameof(ResourceInventoryCollector),
                    nameof(CollectForSubscription),
                    "Skipping file {0}: {1}",
                    file,
                    ex.Message);
            }
        }

        return [.. changes];
    }
}
