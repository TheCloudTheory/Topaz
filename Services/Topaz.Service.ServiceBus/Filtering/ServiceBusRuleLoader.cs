using System.Text.Json;
using System.Xml;
using Topaz.Service.ServiceBus.Models;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Filtering;

/// <summary>
/// Loads subscription rules and subscription names from the filesystem without
/// requiring ARM identifiers (subscription ID, resource group, namespace).  This
/// is used by the AMQP layer at message-publish time, where only the entity address
/// is known.
///
/// The scan is intentionally broad (glob under the emulator root) to stay decoupled
/// from ARM path construction details.
/// </summary>
public sealed class ServiceBusRuleLoader
{
    private readonly string _baseEmulatorPath;
    private readonly ITopazLogger? _logger;

    public ServiceBusRuleLoader(string baseEmulatorPath, ITopazLogger? logger = null)
    {
        _baseEmulatorPath = baseEmulatorPath;
        _logger = logger;
    }

    /// <summary>
    /// Returns <c>true</c> if <paramref name="topicName"/> exists as a persisted
    /// Service Bus topic anywhere under the emulator data directory.
    /// </summary>
    public bool IsKnownTopic(string topicName)
    {
        if (!Directory.Exists(_baseEmulatorPath))
            return false;

        foreach (var dir in EnumerateTopicDirectories(topicName))
        {
            if (Directory.Exists(dir))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the names of all persisted subscriptions under the given topic.
    /// </summary>
    public string[] GetSubscriptionNames(string topicName)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var topicDir in EnumerateTopicDirectories(topicName))
        {
            var subscriptionsDir = Path.Combine(topicDir, "subscriptions");
            if (!Directory.Exists(subscriptionsDir))
                continue;

            foreach (var subDir in Directory.EnumerateDirectories(subscriptionsDir))
            {
                names.Add(Path.GetFileName(subDir));
            }
        }

        return names.ToArray();
    }

    /// <summary>
    /// Loads all persisted rules for the given topic + subscription pair.
    /// Returns an empty array when no rules exist (treat as TrueFilter).
    /// </summary>
    public ServiceBusRuleResourceProperties[] LoadRules(string topicName, string subscriptionName)
    {
        var results = new List<ServiceBusRuleResourceProperties>();

        foreach (var topicDir in EnumerateTopicDirectories(topicName))
        {
            var rulesDir = Path.Combine(topicDir, "subscriptions", subscriptionName, "rules");
            if (!Directory.Exists(rulesDir))
                continue;

            foreach (var ruleDir in Directory.EnumerateDirectories(rulesDir))
            {
                var metadataFile = Path.Combine(ruleDir, "metadata.json");
                if (!File.Exists(metadataFile))
                    continue;

                try
                {
                    var json = File.ReadAllText(metadataFile);
                    var resource = JsonSerializer.Deserialize<ServiceBusRuleResource>(json, GlobalSettings.JsonOptions);
                    if (resource?.Properties != null)
                        results.Add(resource.Properties);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to deserialize rule file '{metadataFile}': {ex.Message}", ex);
                }
            }
        }

        return results.ToArray();
    }

    // Produces all `{emulatorRoot}/.../topics/{topicName}` directories across all
    // subscription / resource-group / namespace combinations under the emulator root.
    // Actual path: {base}/.subscription/{sub}/.resource-group/{rg}/.service-bus/{namespace}/topics/{topicName}
    private IEnumerable<string> EnumerateTopicDirectories(string topicName)
    {
        if (!Directory.Exists(_baseEmulatorPath))
            yield break;

        var subscriptionsRoot = Path.Combine(_baseEmulatorPath, ".subscription");
        if (!Directory.Exists(subscriptionsRoot))
            yield break;

        foreach (var subDir in SafeEnumerateDirectories(subscriptionsRoot))
        {
            var resourceGroupsDir = Path.Combine(subDir, ".resource-group");
            if (!Directory.Exists(resourceGroupsDir))
                continue;

            foreach (var rgDir in SafeEnumerateDirectories(resourceGroupsDir))
            foreach (var namespaceDir in SafeEnumerateDirectories(Path.Combine(rgDir, ".service-bus")))
            {
                var topicDir = Path.Combine(namespaceDir, "topics", topicName);
                yield return topicDir;
            }
        }
    }

    /// <summary>
    /// Returns the configured <c>MaxDeliveryCount</c> for the given AMQP entity address
    /// (normalized, e.g. <c>"myqueue"</c> or <c>"mytopic/subscriptions/mysub"</c>).
    /// Returns 10 when the entity cannot be found or deserialization fails.
    /// </summary>
    public int GetMaxDeliveryCount(string entityAddress)
    {
        const int defaultMaxDeliveryCount = 10;

        // Strip DLQ suffix so callers can pass the original entity address.
        var address = entityAddress;
        if (address.EndsWith("/$deadletterqueue", StringComparison.OrdinalIgnoreCase))
            address = address[..^"/$deadletterqueue".Length];

        try
        {
            var subscriptionsIdx = address.IndexOf("/subscriptions/", StringComparison.OrdinalIgnoreCase);
            if (subscriptionsIdx > 0)
            {
                // Topic subscription: "{topic}/subscriptions/{sub}"
                var topicName = address[..subscriptionsIdx];
                var subscriptionName = address[(subscriptionsIdx + "/subscriptions/".Length)..];

                foreach (var topicDir in EnumerateTopicDirectories(topicName))
                {
                    var metadataFile = Path.Combine(topicDir, "subscriptions", subscriptionName, "metadata.json");
                    if (!File.Exists(metadataFile))
                        continue;

                    var json = File.ReadAllText(metadataFile);
                    var resource = JsonSerializer.Deserialize<ServiceBusSubscriptionResource>(json, GlobalSettings.JsonOptions);
                    if (resource?.Properties?.MaxDeliveryCount is { } mdc)
                        return mdc;
                }

                return defaultMaxDeliveryCount;
            }

            // Queue
            foreach (var queueDir in EnumerateQueueDirectories(address))
            {
                var metadataFile = Path.Combine(queueDir, "metadata.json");
                if (!File.Exists(metadataFile))
                    continue;

                var json = File.ReadAllText(metadataFile);
                var resource = JsonSerializer.Deserialize<ServiceBusQueueResource>(json, GlobalSettings.JsonOptions);
                if (resource?.Properties?.MaxDeliveryCount is { } mdc)
                    return mdc;
            }

            return defaultMaxDeliveryCount;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(nameof(ServiceBusRuleLoader), nameof(GetMaxDeliveryCount),
                "Could not read MaxDeliveryCount for '{0}': {1}", entityAddress, ex.Message);
            return defaultMaxDeliveryCount;
        }
    }

    /// <summary>
    /// Returns the <c>DefaultMessageTimeToLive</c> configured for the given entity address.
    /// Returns <see cref="TimeSpan.MaxValue"/> when the entity cannot be found or has no explicit TTL.
    /// </summary>
    public TimeSpan GetDefaultMessageTimeToLive(string entityAddress)
    {
        // Strip DLQ suffix so callers can pass the original entity address.
        var address = entityAddress;
        if (address.EndsWith("/$deadletterqueue", StringComparison.OrdinalIgnoreCase))
            address = address[..^"/$deadletterqueue".Length];

        try
        {
            var subscriptionsIdx = address.IndexOf("/subscriptions/", StringComparison.OrdinalIgnoreCase);
            if (subscriptionsIdx > 0)
            {
                var topicName = address[..subscriptionsIdx];
                var subscriptionName = address[(subscriptionsIdx + "/subscriptions/".Length)..];

                // Subscriptions inherit DefaultMessageTimeToLive from their parent topic.
                foreach (var topicDir in EnumerateTopicDirectories(topicName))
                {
                    var topicMetadata = Path.Combine(topicDir, "metadata.json");
                    if (!File.Exists(topicMetadata)) continue;

                    var resource = JsonSerializer.Deserialize<ServiceBusTopicResource>(
                        File.ReadAllText(topicMetadata), GlobalSettings.JsonOptions);
                    if (resource?.Properties?.DefaultMessageTimeToLive is { } raw)
                        return XmlConvert.ToTimeSpan(raw);
                }
                return TimeSpan.MaxValue;
            }

            foreach (var queueDir in EnumerateQueueDirectories(address))
            {
                var metadataFile = Path.Combine(queueDir, "metadata.json");
                if (!File.Exists(metadataFile)) continue;

                var resource = JsonSerializer.Deserialize<ServiceBusQueueResource>(
                    File.ReadAllText(metadataFile), GlobalSettings.JsonOptions);
                if (resource?.Properties?.DefaultMessageTimeToLive is { } raw)
                    return XmlConvert.ToTimeSpan(raw);
            }
            return TimeSpan.MaxValue;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(nameof(ServiceBusRuleLoader), nameof(GetDefaultMessageTimeToLive),
                "Could not read DefaultMessageTimeToLive for '{0}': {1}", entityAddress, ex.Message);
            return TimeSpan.MaxValue;
        }
    }

    /// <summary>
    /// Returns the <c>DeadLetteringOnMessageExpiration</c> flag for the given entity address.
    /// Returns <c>false</c> when the entity cannot be found.
    /// </summary>
    public bool GetDeadLetteringOnMessageExpiration(string entityAddress)
    {
        var address = entityAddress;
        if (address.EndsWith("/$deadletterqueue", StringComparison.OrdinalIgnoreCase))
            address = address[..^"/$deadletterqueue".Length];

        try
        {
            var subscriptionsIdx = address.IndexOf("/subscriptions/", StringComparison.OrdinalIgnoreCase);
            if (subscriptionsIdx > 0)
            {
                var topicName = address[..subscriptionsIdx];
                var subscriptionName = address[(subscriptionsIdx + "/subscriptions/".Length)..];

                foreach (var topicDir in EnumerateTopicDirectories(topicName))
                {
                    var metadataFile = Path.Combine(topicDir, "subscriptions", subscriptionName, "metadata.json");
                    if (!File.Exists(metadataFile)) continue;

                    var resource = JsonSerializer.Deserialize<ServiceBusSubscriptionResource>(
                        File.ReadAllText(metadataFile), GlobalSettings.JsonOptions);
                    if (resource?.Properties?.DeadLetteringOnMessageExpiration is { } flag)
                        return flag;
                }
                return false;
            }

            foreach (var queueDir in EnumerateQueueDirectories(address))
            {
                var metadataFile = Path.Combine(queueDir, "metadata.json");
                if (!File.Exists(metadataFile)) continue;

                var resource = JsonSerializer.Deserialize<ServiceBusQueueResource>(
                    File.ReadAllText(metadataFile), GlobalSettings.JsonOptions);
                if (resource?.Properties?.DeadLetteringOnMessageExpiration is { } flag)
                    return flag;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(nameof(ServiceBusRuleLoader), nameof(GetDeadLetteringOnMessageExpiration),
                "Could not read DeadLetteringOnMessageExpiration for '{0}': {1}", entityAddress, ex.Message);
            return false;
        }
    }

    // In-memory cache: normalised entity address → RequiresSession flag.
    // Populated on first access per address; stale after entity update (acceptable for emulator).
    private readonly Dictionary<string, bool> _requiresSessionCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns <c>true</c> when the entity at <paramref name="entityAddress"/> was created
    /// with <c>requiresSession = true</c>.  Returns <c>false</c> when the entity cannot be
    /// found or has no session requirement.  Results are cached in-memory to avoid a
    /// filesystem round-trip on every incoming message.
    /// </summary>
    public bool GetRequiresSession(string entityAddress)
    {
        // Strip DLQ suffix so callers can pass the original entity address.
        var address = entityAddress;
        if (address.EndsWith("/$deadletterqueue", StringComparison.OrdinalIgnoreCase))
            address = address[..^"/$deadletterqueue".Length];

        var normalised = address.Trim('/').ToLowerInvariant();
        if (_requiresSessionCache.TryGetValue(normalised, out var cached))
            return cached;

        var result = LoadRequiresSessionFromDisk(normalised);
        _requiresSessionCache[normalised] = result;
        return result;
    }

    /// <summary>Clears the <see cref="GetRequiresSession"/> cache for a given entity (call after create/update).</summary>
    public void InvalidateRequiresSessionCache(string entityAddress)
    {
        var normalised = entityAddress.Trim('/').ToLowerInvariant();
        _requiresSessionCache.Remove(normalised);
    }

    private bool LoadRequiresSessionFromDisk(string normalisedAddress)
    {
        try
        {
            var subscriptionsIdx = normalisedAddress.IndexOf("/subscriptions/", StringComparison.OrdinalIgnoreCase);
            if (subscriptionsIdx > 0)
            {
                var topicName = normalisedAddress[..subscriptionsIdx];
                var subscriptionName = normalisedAddress[(subscriptionsIdx + "/subscriptions/".Length)..];

                foreach (var topicDir in EnumerateTopicDirectories(topicName))
                {
                    var metadataFile = Path.Combine(topicDir, "subscriptions", subscriptionName, "metadata.json");
                    if (!File.Exists(metadataFile))
                        continue;

                    var json = File.ReadAllText(metadataFile);
                    var resource = JsonSerializer.Deserialize<ServiceBusSubscriptionResource>(json, GlobalSettings.JsonOptions);
                    if (resource?.Properties?.RequiresSession is { } rs)
                        return rs == true;
                }
                return false;
            }

            foreach (var queueDir in EnumerateQueueDirectories(normalisedAddress))
            {
                var metadataFile = Path.Combine(queueDir, "metadata.json");
                if (!File.Exists(metadataFile))
                    continue;

                var json = File.ReadAllText(metadataFile);
                var resource = JsonSerializer.Deserialize<ServiceBusQueueResource>(json, GlobalSettings.JsonOptions);
                if (resource?.Properties?.RequiresSession is { } rs)
                    return rs;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(nameof(ServiceBusRuleLoader), nameof(LoadRequiresSessionFromDisk),
                "Could not read RequiresSession for '{0}': {1}", normalisedAddress, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Returns the <c>ForwardDeadLetteredMessagesTo</c> target entity name for the given entity
    /// address, or <c>null</c> when the property is absent or the entity cannot be found.
    /// </summary>
    public string? GetForwardDeadLetteredMessagesTo(string entityAddress)
    {
        var address = entityAddress;
        if (address.EndsWith("/$deadletterqueue", StringComparison.OrdinalIgnoreCase))
            address = address[..^"/$deadletterqueue".Length];

        try
        {
            var subscriptionsIdx = address.IndexOf("/subscriptions/", StringComparison.OrdinalIgnoreCase);
            if (subscriptionsIdx > 0)
            {
                var topicName = address[..subscriptionsIdx];
                var subscriptionName = address[(subscriptionsIdx + "/subscriptions/".Length)..];

                foreach (var topicDir in EnumerateTopicDirectories(topicName))
                {
                    var metadataFile = Path.Combine(topicDir, "subscriptions", subscriptionName, "metadata.json");
                    if (!File.Exists(metadataFile)) continue;

                    var resource = JsonSerializer.Deserialize<ServiceBusSubscriptionResource>(
                        File.ReadAllText(metadataFile), GlobalSettings.JsonOptions);
                    if (!string.IsNullOrEmpty(resource?.Properties?.ForwardDeadLetteredMessagesTo))
                        return resource.Properties.ForwardDeadLetteredMessagesTo;
                }
                return null;
            }

            foreach (var queueDir in EnumerateQueueDirectories(address))
            {
                var metadataFile = Path.Combine(queueDir, "metadata.json");
                if (!File.Exists(metadataFile)) continue;

                var resource = JsonSerializer.Deserialize<ServiceBusQueueResource>(
                    File.ReadAllText(metadataFile), GlobalSettings.JsonOptions);
                if (!string.IsNullOrEmpty(resource?.Properties?.ForwardDeadLetteredMessagesTo))
                    return resource.Properties.ForwardDeadLetteredMessagesTo;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(nameof(ServiceBusRuleLoader), nameof(GetForwardDeadLetteredMessagesTo),
                "Could not read ForwardDeadLetteredMessagesTo for '{0}': {1}", entityAddress, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Returns <c>true</c> when a queue with the given name exists anywhere under the emulator
    /// data directory.  Used to validate a <c>ForwardDeadLetteredMessagesTo</c> target before routing.
    /// </summary>
    public bool IsKnownQueue(string queueName)
    {
        foreach (var dir in EnumerateQueueDirectories(queueName))
        {
            if (File.Exists(Path.Combine(dir, "metadata.json")))
                return true;
        }
        return false;
    }

    // Produces all `{emulatorRoot}/.../queues/{queueName}` directories.
    private IEnumerable<string> EnumerateQueueDirectories(string queueName)
    {
        if (!Directory.Exists(_baseEmulatorPath))
            yield break;

        var subscriptionsRoot = Path.Combine(_baseEmulatorPath, ".subscription");
        if (!Directory.Exists(subscriptionsRoot))
            yield break;

        foreach (var subDir in SafeEnumerateDirectories(subscriptionsRoot))
        {
            var resourceGroupsDir = Path.Combine(subDir, ".resource-group");
            if (!Directory.Exists(resourceGroupsDir))
                continue;

            foreach (var rgDir in SafeEnumerateDirectories(resourceGroupsDir))
            foreach (var namespaceDir in SafeEnumerateDirectories(Path.Combine(rgDir, ".service-bus")))
            {
                yield return Path.Combine(namespaceDir, "queues", queueName);
            }
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string path)
    {
        try
        {
            return Directory.EnumerateDirectories(path);
        }
        catch (Exception)
        {
            return Enumerable.Empty<string>();
        }
    }
}
