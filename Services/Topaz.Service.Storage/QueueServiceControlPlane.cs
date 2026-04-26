using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class QueueServiceControlPlane(QueueResourceProvider provider, ITopazLogger logger)
{
    public static QueueServiceControlPlane New(ITopazLogger logger) => new(new QueueResourceProvider(logger), logger);
    public ControlPlaneOperationResult<QueueProperties[]> ListQueues(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        var queues = provider.ListAs<Queue>(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, 10);
        var result = queues.Select(queue => new QueueProperties
        {
            Name = queue.Name,
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow,
            ApproximateMessageCount = 0,
            Metadata = new Dictionary<string, string>()
        }).ToArray()!;

        return new ControlPlaneOperationResult<QueueProperties[]>(OperationResult.Success, result, null, null);
    }

    public ControlPlaneOperationResult<Queue> CreateQueue(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        var now = DateTimeOffset.UtcNow;
        var queue = new Queue
        {
            Name = queueName,
            Properties = new QueueProperties
            {
                Name = queueName,
                CreatedTime = now,
                UpdatedTime = now,
                ApproximateMessageCount = 0,
                Metadata = new Dictionary<string, string>()
            }
        };

        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName, queue);

        // Persist properties
        var propertiesPath = provider.GetQueuePropertiesFilePath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);
        File.WriteAllText(propertiesPath, JsonSerializer.Serialize(queue.Properties, GlobalSettings.JsonOptions));

        return new ControlPlaneOperationResult<Queue>(OperationResult.Created, queue, null, null);
    }

    public ControlPlaneOperationResult DeleteQueue(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName);
        return new ControlPlaneOperationResult(OperationResult.Success);
    }

    public bool QueueExists(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        return provider.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName);
    }

    public ControlPlaneOperationResult SetQueueMetadata(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName,
        Dictionary<string, string> metadata)
    {
        if (!provider.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName))
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, "Queue not found.", "QueueNotFound");
        }

        var propertiesPath = provider.GetQueuePropertiesFilePath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        QueueProperties properties;
        if (File.Exists(propertiesPath))
        {
            var content = File.ReadAllText(propertiesPath);
            properties = JsonSerializer.Deserialize<QueueProperties>(content, GlobalSettings.JsonOptions)
                ?? new QueueProperties();
        }
        else
        {
            properties = new QueueProperties { Name = queueName };
        }

        properties.Metadata = metadata;
        properties.UpdatedTime = DateTimeOffset.UtcNow;

        File.WriteAllText(propertiesPath, JsonSerializer.Serialize(properties, GlobalSettings.JsonOptions));

        return new ControlPlaneOperationResult(OperationResult.Success);
    }

    public ControlPlaneOperationResult<QueueProperties> GetQueueProperties(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        if (!provider.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName))
        {
            return new ControlPlaneOperationResult<QueueProperties>(OperationResult.NotFound, null,
                "Queue not found.", "QueueNotFound");
        }

        var propertiesPath = provider.GetQueuePropertiesFilePath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        if (!File.Exists(propertiesPath))
        {
            return new ControlPlaneOperationResult<QueueProperties>(OperationResult.NotFound, null,
                "Queue properties file not found.", "QueuePropertiesNotFound");
        }

        var content = File.ReadAllText(propertiesPath);
        var properties = JsonSerializer.Deserialize<QueueProperties>(content, GlobalSettings.JsonOptions);

        return new ControlPlaneOperationResult<QueueProperties>(OperationResult.Success, properties, null, null);
    }
}
