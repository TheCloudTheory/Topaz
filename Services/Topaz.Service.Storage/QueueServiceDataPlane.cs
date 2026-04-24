using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class QueueServiceDataPlane(QueueServiceControlPlane controlPlane, QueueResourceProvider resourceProvider, ITopazLogger logger)
{
    public static QueueServiceDataPlane New(ITopazLogger logger)
    {
        var resourceProvider = new QueueResourceProvider(logger);
        var controlPlane = QueueServiceControlPlane.New(logger);
        return new QueueServiceDataPlane(controlPlane, resourceProvider, logger);
    }
    public DataPlaneOperationResult<QueueEnumerationResult> ListQueues(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(ListQueues),
            "Executing {0}: {1}", nameof(ListQueues), storageAccountName);

        var result = controlPlane.ListQueues(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (result.Result == OperationResult.Success && result.Resource != null)
        {
            var queueEnumeration = new QueueEnumerationResult(storageAccountName, result.Resource);
            return new DataPlaneOperationResult<QueueEnumerationResult>(OperationResult.Success, queueEnumeration,
                null, null);
        }

        return new DataPlaneOperationResult<QueueEnumerationResult>(OperationResult.Failed, null,
            "Failed to list queues.", null);
    }

    public DataPlaneOperationResult<Queue> CreateQueue(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(CreateQueue),
            "Executing {0}: {1} {2}", nameof(CreateQueue), storageAccountName, queueName);

        if (controlPlane.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
                queueName))
        {
            return new DataPlaneOperationResult<Queue>(OperationResult.Conflict, null,
                "Queue already exists.", "QueueAlreadyExists");
        }

        var result = controlPlane.CreateQueue(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        return new DataPlaneOperationResult<Queue>(result.Result, result.Resource, result.Reason,
            result.Code);
    }

    public DataPlaneOperationResult DeleteQueue(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(DeleteQueue),
            "Executing {0}: {1} {2}", nameof(DeleteQueue), storageAccountName, queueName);

        if (!controlPlane.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
                queueName))
        {
            return new DataPlaneOperationResult(OperationResult.NotFound, "Queue not found.", "QueueNotFound");
        }

        var result = controlPlane.DeleteQueue(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        return new DataPlaneOperationResult(result.Result, result.Reason, result.Code);
    }

    public DataPlaneOperationResult<QueueProperties> GetQueueProperties(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName, string queueName)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(GetQueueProperties),
            "Executing {0}: {1} {2}", nameof(GetQueueProperties), storageAccountName, queueName);

        var result = controlPlane.GetQueueProperties(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        return new DataPlaneOperationResult<QueueProperties>(result.Result, result.Resource, result.Reason,
            result.Code);
    }

    public DataPlaneOperationResult<QueueMessage> PutMessage(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName,
        string messageId, string content, int visibilityTimeout = 30)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(PutMessage),
            "Executing {0}: {1} {2} {3}", nameof(PutMessage), storageAccountName, queueName, messageId);

        if (!controlPlane.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName))
        {
            return new DataPlaneOperationResult<QueueMessage>(OperationResult.NotFound, null,
                "Queue not found.", "QueueNotFound");
        }

        var messageDir = resourceProvider.GetMessagesDirectoryPath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);
        Directory.CreateDirectory(messageDir);

        var messagePath = resourceProvider.GetMessageFilePath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName, messageId);

        QueueMessage message;

        if (File.Exists(messagePath))
        {
            var existingContent = File.ReadAllText(messagePath);
            message = JsonSerializer.Deserialize<QueueMessage>(existingContent, GlobalSettings.JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize message");
            
            // Update existing message
            message.UpdateContent(content);
            message.UpdateVisibility(visibilityTimeout);
        }
        else
        {
            // Create new message
            message = new QueueMessage(messageId, content);
            message.UpdateVisibility(visibilityTimeout);
            if (message.EnqueuedTime.HasValue && message.TimeToLive > 0)
            {
                message.ExpiryTime = message.EnqueuedTime.Value.AddSeconds(message.TimeToLive);
            }
        }

        // Persist message
        File.WriteAllText(messagePath, JsonSerializer.Serialize(message, GlobalSettings.JsonOptions));

        return new DataPlaneOperationResult<QueueMessage>(OperationResult.Success, message, null, null);
    }

    public DataPlaneOperationResult<QueueMessage> GetMessage(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName, string messageId)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(GetMessage),
            "Executing {0}: {1} {2} {3}", nameof(GetMessage), storageAccountName, queueName, messageId);

        if (!controlPlane.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName))
        {
            return new DataPlaneOperationResult<QueueMessage>(OperationResult.NotFound, null,
                "Queue not found.", "QueueNotFound");
        }

        var messagePath = resourceProvider.GetMessageFilePath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName, messageId);

        if (!File.Exists(messagePath))
        {
            return new DataPlaneOperationResult<QueueMessage>(OperationResult.NotFound, null,
                "Message not found.", "MessageNotFound");
        }

        var messageContent = File.ReadAllText(messagePath);
        var message = JsonSerializer.Deserialize<QueueMessage>(messageContent, GlobalSettings.JsonOptions);

        if (message == null)
        {
            return new DataPlaneOperationResult<QueueMessage>(OperationResult.Failed, null,
                "Failed to deserialize message.", "DeserializationError");
        }

        // Check if message has expired
        if (message.IsExpired())
        {
            File.Delete(messagePath);
            return new DataPlaneOperationResult<QueueMessage>(OperationResult.NotFound, null,
                "Message has expired.", "MessageExpired");
        }

        return new DataPlaneOperationResult<QueueMessage>(OperationResult.Success, message, null, null);
    }

    public DataPlaneOperationResult DeleteMessage(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName, string messageId)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(DeleteMessage),
            "Executing {0}: {1} {2} {3}", nameof(DeleteMessage), storageAccountName, queueName, messageId);

        if (!controlPlane.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName))
        {
            return new DataPlaneOperationResult(OperationResult.NotFound,
                "Queue not found.", "QueueNotFound");
        }

        var messagePath = resourceProvider.GetMessageFilePath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName, messageId);

        if (!File.Exists(messagePath))
        {
            return new DataPlaneOperationResult(OperationResult.NotFound,
                "Message not found.", "MessageNotFound");
        }

        File.Delete(messagePath);
        return new DataPlaneOperationResult(OperationResult.Success, null, null);
    }

    public DataPlaneOperationResult<QueueMessage> PeekMessage(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName, string messageId)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(PeekMessage),
            "Executing {0}: {1} {2} {3}", nameof(PeekMessage), storageAccountName, queueName, messageId);

        // Peek is similar to Get but without incrementing dequeue count or affecting visibility
        return GetMessage(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName, messageId);
    }
}

public sealed class QueueEnumerationResult
{
    public string? StorageAccountName { get; set; }
    public QueueProperties[]? Queues { get; set; }

    public QueueEnumerationResult(string storageAccountName, QueueProperties[] queues)
    {
        StorageAccountName = storageAccountName;
        Queues = queues;
    }
}
