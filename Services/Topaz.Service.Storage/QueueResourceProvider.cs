using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Services;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class QueueResourceProvider(ITopazLogger logger) : ResourceProviderBase<QueueStorageService>(logger)
{
    private readonly ITopazLogger _logger = logger;

    public bool QueueExists(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        _logger.LogDebug(nameof(QueueResourceProvider), nameof(QueueExists),
            "Executing for {0}, {1}, {2}, {3}", subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName);

        var queuePath = GetQueuePathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);
        return Directory.Exists(queuePath);
    }

    public void Create(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName, string queueName, Queue queue)
    {
        base.Create(subscriptionIdentifier, resourceGroupIdentifier,
            GetQueueId(storageAccountName, queueName), queue);

        var metadata = Path.Combine(GetQueueMetadataPath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName));
        Directory.CreateDirectory(metadata);

        var data = GetQueueDataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName);
        Directory.CreateDirectory(data);
    }

    public void Delete(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName, string queueName)
    {
        base.Delete(subscriptionIdentifier, resourceGroupIdentifier, GetQueueId(storageAccountName, queueName));
    }

    public string GetQueueDataPath(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        return Path.Combine(GetQueuePathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName), "data");
    }

    public string GetQueueMetadataPath(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        var queuePath = GetQueuePathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        return Path.Combine(queuePath, ".metadata");
    }

    public string GetQueuePropertiesFilePath(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        return Path.Combine(
            GetQueuePathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName),
            ".queue-properties.json");
    }

    public string GetMessageFilePath(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName, string messageId)
    {
        var messagesPath = Path.Combine(GetQueueDataPath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName), "messages");
        return Path.Combine(messagesPath, $"{messageId}.json");
    }

    public string GetQueueAclFilePath(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        return Path.Combine(
            GetQueuePathWithReplacedValues(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName),
            ".acl.xml");
    }

    public string GetMessagesDirectoryPath(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        return Path.Combine(GetQueueDataPath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName), "messages");
    }

    private string GetQueuePathWithReplacedValues(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        var storageAccountPath =
            GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);

        return Path.Combine(storageAccountPath, ".queue", queueName);
    }

    private static string GetQueueId(string storageAccountName, string queueName)
    {
        return Path.Combine(storageAccountName, ".queue", queueName);
    }
}
