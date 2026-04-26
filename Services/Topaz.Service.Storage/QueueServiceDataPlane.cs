using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Utils;
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

        if (result.Result == OperationResult.Success && result.Resource != null)
        {
            var messagesDir = resourceProvider.GetMessagesDirectoryPath(
                subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName);
            result.Resource.ApproximateMessageCount = Directory.Exists(messagesDir)
                ? Directory.GetFiles(messagesDir, "*.json").Length
                : 0;
        }

        return new DataPlaneOperationResult<QueueProperties>(result.Result, result.Resource, result.Reason,
            result.Code);
    }

    public DataPlaneOperationResult<string> GetQueueAcl(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(GetQueueAcl),
            "Executing {0}: {1} {2}", nameof(GetQueueAcl), storageAccountName, queueName);

        var (exists, aclFilePath) = controlPlane.GetQueueAclState(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        if (!exists)
            return new DataPlaneOperationResult<string>(OperationResult.NotFound, null, "Queue not found.", "QueueNotFound");

        if (!File.Exists(aclFilePath))
            return new DataPlaneOperationResult<string>(OperationResult.Success,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><SignedIdentifiers />", null, null);

        var xml = File.ReadAllText(aclFilePath);
        return new DataPlaneOperationResult<string>(OperationResult.Success, xml, null, null);
    }

    public DataPlaneOperationResult SetQueueAcl(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName,
        Stream requestBody)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(SetQueueAcl),
            "Executing {0}: {1} {2}", nameof(SetQueueAcl), storageAccountName, queueName);

        var (exists, aclFilePath) = controlPlane.GetQueueAclState(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        if (!exists)
            return new DataPlaneOperationResult(OperationResult.NotFound, "Queue not found.", "QueueNotFound");

        using var reader = new StreamReader(requestBody);
        var body = reader.ReadToEnd();

        if (string.IsNullOrWhiteSpace(body))
            body = "<?xml version=\"1.0\" encoding=\"utf-8\"?><SignedIdentifiers />";

        File.WriteAllText(aclFilePath, body);
        return new DataPlaneOperationResult(OperationResult.Success, null, null);
    }

    public DataPlaneOperationResult SetQueueMetadata(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName,
        IEnumerable<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> headers)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(SetQueueMetadata),
            "Executing {0}: {1} {2}", nameof(SetQueueMetadata), storageAccountName, queueName);

        var metadata = headers
            .Where(h => h.Key.StartsWith("x-ms-meta-", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(h => h.Key["x-ms-meta-".Length..], h => h.Value.ToString());

        var result = controlPlane.SetQueueMetadata(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName, metadata);

        return new DataPlaneOperationResult(result.Result, result.Reason, result.Code);
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

    public DataPlaneOperationResult ClearMessages(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(ClearMessages),
            "Executing {0}: {1} {2}", nameof(ClearMessages), storageAccountName, queueName);

        if (!controlPlane.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName))
        {
            return new DataPlaneOperationResult(OperationResult.NotFound, "Queue not found.", "QueueNotFound");
        }

        var messageDir = resourceProvider.GetMessagesDirectoryPath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        if (Directory.Exists(messageDir))
        {
            foreach (var file in Directory.GetFiles(messageDir, "*.json"))
                File.Delete(file);
        }

        return new DataPlaneOperationResult(OperationResult.Success, null, null);
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

    public DataPlaneOperationResult<List<QueueMessage>> GetMessages(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName,
        int numMessages = 1, int visibilityTimeout = 30)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(GetMessages),
            "Executing {0}: {1} {2} numMessages={3} visibilityTimeout={4}", 
            nameof(GetMessages), storageAccountName, queueName, numMessages, visibilityTimeout);

        if (!controlPlane.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName))
        {
            return new DataPlaneOperationResult<List<QueueMessage>>(OperationResult.NotFound, null,
                "Queue not found.", "QueueNotFound");
        }

        var messageDir = resourceProvider.GetMessagesDirectoryPath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        if (!Directory.Exists(messageDir))
        {
            return new DataPlaneOperationResult<List<QueueMessage>>(OperationResult.Success, 
                new List<QueueMessage>(), null, null);
        }

        var messages = new List<QueueMessage>();
        var messageFiles = Directory.GetFiles(messageDir, "*.json").OrderBy(f => f).ToArray();

        foreach (var filePath in messageFiles)
        {
            if (messages.Count >= numMessages)
                break;

            try
            {
                var messageContent = File.ReadAllText(filePath);
                var message = JsonSerializer.Deserialize<QueueMessage>(messageContent, GlobalSettings.JsonOptions);

                if (message == null)
                    continue;

                // Skip if expired
                if (message.IsExpired())
                {
                    File.Delete(filePath);
                    continue;
                }

                // Skip if not visible yet
                if (!message.IsVisible())
                    continue;

                // Message is visible and not expired - prepare for return
                message.DequeueCount++;
                message.UpdateVisibility(visibilityTimeout);

                // Persist the updated message
                File.WriteAllText(filePath, JsonSerializer.Serialize(message, GlobalSettings.JsonOptions));

                messages.Add(message);
            }
            catch (Exception ex)
            {
                logger.LogError(nameof(QueueServiceDataPlane), nameof(GetMessages),
                    "Error processing message file {0}: {1}", filePath, ex.Message);
            }
        }

        return new DataPlaneOperationResult<List<QueueMessage>>(OperationResult.Success, messages, null, null);
    }

    public DataPlaneOperationResult<List<QueueMessage>> PeekMessages(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName,
        int numMessages = 1)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(PeekMessages),
            "Executing {0}: {1} {2} numMessages={3}",
            nameof(PeekMessages), storageAccountName, queueName, numMessages);

        if (!controlPlane.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName))
        {
            return new DataPlaneOperationResult<List<QueueMessage>>(OperationResult.NotFound, null,
                "Queue not found.", "QueueNotFound");
        }

        var messageDir = resourceProvider.GetMessagesDirectoryPath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        if (!Directory.Exists(messageDir))
        {
            return new DataPlaneOperationResult<List<QueueMessage>>(OperationResult.Success,
                new List<QueueMessage>(), null, null);
        }

        var messages = new List<QueueMessage>();
        var messageFiles = Directory.GetFiles(messageDir, "*.json").OrderBy(f => f).ToArray();

        foreach (var filePath in messageFiles)
        {
            if (messages.Count >= numMessages)
                break;

            try
            {
                var messageContent = File.ReadAllText(filePath);
                var message = JsonSerializer.Deserialize<QueueMessage>(messageContent, GlobalSettings.JsonOptions);

                if (message == null)
                    continue;

                if (message.IsExpired())
                {
                    File.Delete(filePath);
                    continue;
                }

                if (!message.IsVisible())
                    continue;

                messages.Add(message);
            }
            catch (Exception ex)
            {
                logger.LogError(nameof(QueueServiceDataPlane), nameof(PeekMessages),
                    "Error processing message file {0}: {1}", filePath, ex.Message);
            }
        }

        return new DataPlaneOperationResult<List<QueueMessage>>(OperationResult.Success, messages, null, null);
    }

    public DataPlaneOperationResult<QueueMessage> SendMessage(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName,
        string messageContent, int visibilityTimeout = 0, int messageTtl = 604800)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(SendMessage),
            "Executing {0}: {1} {2} visibilityTimeout={3} messageTtl={4}",
            nameof(SendMessage), storageAccountName, queueName, visibilityTimeout, messageTtl);

        if (!controlPlane.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName))
        {
            return new DataPlaneOperationResult<QueueMessage>(OperationResult.NotFound, null,
                "Queue not found.", "QueueNotFound");
        }

        // Validate message size
        if (!QueueMessageValidator.ValidateMessageSize(messageContent, out var sizeError))
        {
            logger.LogDebug(nameof(QueueServiceDataPlane), nameof(SendMessage),
                "Message size validation failed: {0}", sizeError);
            return new DataPlaneOperationResult<QueueMessage>(OperationResult.Failed, null,
                sizeError, "MessageTooLarge");
        }

        // Validate visibility timeout
        if (!QueueMessageValidator.ValidateVisibilityTimeout(visibilityTimeout, out var visibilityError))
        {
            logger.LogDebug(nameof(QueueServiceDataPlane), nameof(SendMessage),
                "Visibility timeout validation failed: {0}", visibilityError);
            return new DataPlaneOperationResult<QueueMessage>(OperationResult.Failed, null,
                visibilityError, "InvalidVisibilityTimeout");
        }

        var messageDir = resourceProvider.GetMessagesDirectoryPath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);
        Directory.CreateDirectory(messageDir);

        // Generate unique message ID (GUID)
        var messageId = Guid.NewGuid().ToString();
        var messagePath = resourceProvider.GetMessageFilePath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName, messageId);

        // Create new message
        var message = new QueueMessage(messageId, messageContent)
        {
            VisibilityTimeout = visibilityTimeout,
            TimeToLive = messageTtl
        };

        // Set visibility timeout if specified
        if (visibilityTimeout > 0)
        {
            message.UpdateVisibility(visibilityTimeout);
        }

        // Calculate expiry time
        if (message.EnqueuedTime.HasValue && messageTtl > 0)
        {
            message.ExpiryTime = message.EnqueuedTime.Value.AddSeconds(messageTtl);
        }

        // Persist message
        File.WriteAllText(messagePath, JsonSerializer.Serialize(message, GlobalSettings.JsonOptions));

        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(SendMessage),
            "Message {0} enqueued to queue {1}", messageId, queueName);

        return new DataPlaneOperationResult<QueueMessage>(OperationResult.Success, message, null, null);
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
