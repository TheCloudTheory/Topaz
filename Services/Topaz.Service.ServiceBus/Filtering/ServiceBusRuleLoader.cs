using System.Text.Json;
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

    public ServiceBusRuleLoader(string baseEmulatorPath)
    {
        _baseEmulatorPath = baseEmulatorPath;
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
        catch (Exception)
        {
            return defaultMaxDeliveryCount;
        }
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
