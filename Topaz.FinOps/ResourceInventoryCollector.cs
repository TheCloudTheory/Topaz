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

                if (root.ValueKind != JsonValueKind.Object ||
                    !root.TryGetProperty("id", out var idElement) ||
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

                // Populate properties so ACE estimators can read resource-specific config
                // (e.g. hardwareProfile.vmSize for VMs, sku.name for storage accounts, etc.)
                IDictionary<string, object?>? properties = null;
                if (root.TryGetProperty("properties", out var propsElement) &&
                    propsElement.ValueKind == JsonValueKind.Object)
                {
                    properties = new Dictionary<string, object?>();
                    foreach (var prop in propsElement.EnumerateObject())
                    {
                        properties[prop.Name] = prop.Value.Clone();
                    }
                }

                WhatIfSku? sku = null;
                if (root.TryGetProperty("sku", out var skuElement) &&
                    skuElement.ValueKind == JsonValueKind.Object)
                {
                    skuElement.TryGetProperty("name", out var skuName);
                    skuElement.TryGetProperty("tier", out var skuTier);
                    skuElement.TryGetProperty("capacity", out var skuCapacity);
                    sku = new WhatIfSku
                    {
                        name = skuName.ValueKind == JsonValueKind.String ? skuName.GetString() : null,
                        tier = skuTier.ValueKind == JsonValueKind.String ? skuTier.GetString() : null,
                        capacity = skuCapacity.ValueKind == JsonValueKind.Number ? skuCapacity.GetInt32() : null
                    };
                }

                string? kind = root.TryGetProperty("kind", out var kindElement)
                    ? kindElement.GetString()
                    : null;

                changes.Add(new WhatIfChange
                {
                    resourceId = resourceId,
                    changeType = WhatIfChangeType.Create,
                    after = new WhatIfAfterBeforeChange
                    {
                        type = resourceType,
                        location = location,
                        properties = properties,
                        sku = sku,
                        kind = kind
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

        // Deduplicate by resourceId — the same resource can appear in multiple
        // JSON files on disk (e.g. stored both as a subresource entry and inside
        // its parent). ACE's ReconcileResources uses a Dictionary and throws on
        // duplicate keys, so we keep only the first occurrence.
        return [.. changes
            .GroupBy(c => c.resourceId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())];
    }}
