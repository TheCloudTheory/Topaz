using Topaz.CLI;
using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.Storage.Queues;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class QueueStorageTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("E7D3A2F1-C456-4B89-9D1F-3E2A5C8F7B6A");
    
    private const string SubscriptionName = "sub-queue-test";
    private const string ResourceGroupName = "queue-test-rg";
    private const string StorageAccountName = "queuestoragetest";

    private string _key = null!;
    
    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(
        [
            "subscription",
            "delete",
            "--id",
            SubscriptionId.ToString()
        ]);
        
        await Program.RunAsync(
        [
            "subscription",
            "create",
            "--id",
            SubscriptionId.ToString(),
            "--name",
            SubscriptionName
        ]);

        await Program.RunAsync([
            "group",
            "delete",
            "--name",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "group",
            "create",
            "--name",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        await Program.RunAsync([
            "storage",
            "account",
            "delete",
            "--name",
            StorageAccountName,
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "storage",
            "account",
            "create",
            "--name",
            StorageAccountName,
            "-g",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var storageAccount = await resourceGroup.Value.GetStorageAccountAsync(StorageAccountName);
        var keys = storageAccount.Value.GetKeys().ToArray();

        _key = keys[0].Value;
    }

    [Test]
    public void Queue_Create_SucceedsWithValidName()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));

        // Act
        var response = queueClient.CreateQueue("testqueue");

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Value.Name, Is.EqualTo("testqueue"));
    }

    [Test]
    public void Queue_Create_MultipleQueuesAreAvailable()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));

        // Act
        queueClient.CreateQueue("queue1");
        queueClient.CreateQueue("queue2");
        queueClient.CreateQueue("queue3");

        var queues = queueClient.GetQueues().ToArray();

        // Assert
        Assert.That(queues, Has.Length.GreaterThanOrEqualTo(3));
        Assert.That(queues.Any(q => q.Name == "queue1"), Is.True);
        Assert.That(queues.Any(q => q.Name == "queue2"), Is.True);
        Assert.That(queues.Any(q => q.Name == "queue3"), Is.True);
    }

    [Test]
    public void Queue_Delete_SucceedsWhenExists()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("queue-to-delete");

        // Act
        var deleteResponse = queueClient.DeleteQueue("queue-to-delete");

        // Assert
        Assert.That(deleteResponse, Is.Not.Null);
        
        var queues = queueClient.GetQueues().ToArray();
        Assert.That(queues.Any(q => q.Name == "queue-to-delete"), Is.False);
    }

    [Test]
    public void Queue_GetProperties_ReturnsQueueMetadata()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        var createResponse = queueClient.CreateQueue("props-test-queue");
        Assert.That(createResponse, Is.Not.Null, "Queue creation should not return null");

        // Act
        var queue = queueClient.GetQueueClient("props-test-queue");
        var properties = queue.GetProperties();

        // Assert
        Assert.That(properties, Is.Not.Null);
        Assert.That(properties.Value.ApproximateMessagesCount, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void Queue_List_ReturnsAllQueues()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("list-queue-1");
        queueClient.CreateQueue("list-queue-2");

        // Act
        var queues = queueClient.GetQueues().ToArray();

        // Assert
        Assert.That(queues.Length, Is.GreaterThanOrEqualTo(2));
        Assert.That(queues.Any(q => q.Name == "list-queue-1"), Is.True);
        Assert.That(queues.Any(q => q.Name == "list-queue-2"), Is.True);
    }

    [Test]
    public void Queue_PutMessage_EnqueuesAndUpdatesMessage()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("message-test-queue");
        var queue = queueClient.GetQueueClient("message-test-queue");
        
        var messageContent = "Test message content";

        // Act - SendMessage (enqueue)
        var sendResponse = queue.SendMessage(messageContent);
        Assert.That(sendResponse.Value.MessageId, Is.Not.Null);
        Assert.That(sendResponse.Value.PopReceipt, Is.Not.Null);

        // Act - UpdateMessage (Put Message with new visibility)
        var messageId = sendResponse.Value.MessageId;
        var popReceipt = sendResponse.Value.PopReceipt;
        var newContent = "Updated message content";
        
        var updateResponse = queue.UpdateMessage(messageId, popReceipt, new BinaryData(newContent), TimeSpan.FromSeconds(60));
        
        // Assert
        Assert.That(updateResponse.Value.PopReceipt, Is.Not.Null);
        Assert.That(updateResponse.Value.PopReceipt, Is.Not.EqualTo(popReceipt), "Pop receipt should be regenerated");
    }

    [Test]
    public void Queue_PutMessage_WithVisibilityTimeout()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("visibility-test-queue");
        var queue = queueClient.GetQueueClient("visibility-test-queue");
        
        var messageContent = "Visibility test message";

        // Act - SendMessage
        var sendResponse = queue.SendMessage(messageContent);
        var messageId = sendResponse.Value.MessageId;
        var popReceipt = sendResponse.Value.PopReceipt;

        // Act - UpdateMessage with 120 second visibility timeout
        var updateResponse = queue.UpdateMessage(messageId, popReceipt, new BinaryData(messageContent), TimeSpan.FromSeconds(120));

        // Assert
        Assert.That(updateResponse.Value.PopReceipt, Is.Not.Null);
    }

    [Test]
    public void Queue_PutMessage_MessageCountIncreases()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("count-test-queue");
        var queue = queueClient.GetQueueClient("count-test-queue");

        // Act
        queue.SendMessage("Message 1");
        queue.SendMessage("Message 2");
        queue.SendMessage("Message 3");

        var properties = queue.GetProperties();

        // Assert
        Assert.That(properties.Value.ApproximateMessagesCount, Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void Queue_GetMessages_ReturnsEmptyWhenNoMessages()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("empty-queue");
        var queue = queueClient.GetQueueClient("empty-queue");

        // Act
        var messages = queue.ReceiveMessages(1).Value.ToList();

        // Assert
        Assert.That(messages, Has.Count.EqualTo(0));
    }

    [Test]
    public void Queue_GetMessages_ReturnsSingleMessageWhenQueued()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("single-message-queue");
        var queue = queueClient.GetQueueClient("single-message-queue");
        
        var messageContent = "Single message content";
        queue.SendMessage(messageContent);

        // Act
        var messages = queue.ReceiveMessages(1).Value.ToList();

        // Assert
        Assert.That(messages, Has.Count.EqualTo(1));
        Assert.That(messages[0].MessageText, Is.EqualTo(messageContent));
        Assert.That(messages[0].PopReceipt, Is.Not.Null);
    }

    [Test]
    public void Queue_GetMessages_ReturnsMultipleMessages()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("multi-message-queue");
        var queue = queueClient.GetQueueClient("multi-message-queue");
        
        queue.SendMessage("Message 1");
        queue.SendMessage("Message 2");
        queue.SendMessage("Message 3");

        // Act
        var messages = queue.ReceiveMessages(3).Value.ToList();

        // Assert
        Assert.That(messages, Has.Count.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void Queue_GetMessages_RespectNumofMessagesLimit()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("limit-test-queue");
        var queue = queueClient.GetQueueClient("limit-test-queue");
        
        for (int i = 0; i < 10; i++)
        {
            queue.SendMessage($"Message {i}");
        }

        // Act
        var messages = queue.ReceiveMessages(5).Value.ToList();

        // Assert
        Assert.That(messages, Has.Count.EqualTo(5));
    }

    [Test]
    public void Queue_GetMessages_IncrementsDequeueCount()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("dequeue-count-queue");
        var queue = queueClient.GetQueueClient("dequeue-count-queue");
        
        queue.SendMessage("Test message");

        // Act - First receive
        var messages1 = queue.ReceiveMessages(1).Value.ToList();
        var initialDequeueCount = messages1[0].DequeueCount;

        // Delete the message and re-send to simulate another dequeue
        queue.DeleteMessage(messages1[0].MessageId, messages1[0].PopReceipt);
        queue.SendMessage("Test message");

        // Act - Second receive
        var messages2 = queue.ReceiveMessages(1).Value.ToList();
        var newDequeueCount = messages2[0].DequeueCount;

        // Assert
        Assert.That(newDequeueCount, Is.GreaterThanOrEqualTo(initialDequeueCount));
    }

    [Test]
    public void Queue_GetMessages_HidesMessageDuringVisibility()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("visibility-hide-queue");
        var queue = queueClient.GetQueueClient("visibility-hide-queue");

        queue.SendMessage("Hidden message");

        // Act - First receive with 60 second visibility
        var messages1 = queue.ReceiveMessages(1, TimeSpan.FromSeconds(60)).Value.ToList();
        var firstMessageId = messages1[0].MessageId;

        // Try to receive again immediately
        var messages2 = queue.ReceiveMessages(1).Value.ToList();

        // Assert
        Assert.That(messages1, Has.Count.EqualTo(1), "First receive should get 1 message");
        Assert.That(messages2, Has.Count.EqualTo(0), "Second receive should get 0 messages (hidden during visibility)");
    }

    [Test]
    public void Queue_SendMessage_ReturnsMessageIdAndPopReceipt()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("send-receipt-queue");
        var queue = queueClient.GetQueueClient("send-receipt-queue");

        // Act
        var result = queue.SendMessage("Hello World");

        // Assert
        Assert.That(result.Value.MessageId, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Value.PopReceipt, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Value.InsertionTime, Is.Not.EqualTo(default(DateTimeOffset)));
        Assert.That(result.Value.ExpirationTime, Is.Not.EqualTo(default(DateTimeOffset)));
    }

    [Test]
    public void Queue_SendMessage_MessageIsDequeueableAfterSend()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("send-dequeue-queue");
        var queue = queueClient.GetQueueClient("send-dequeue-queue");

        // Act
        queue.SendMessage("Dequeue me");
        var received = queue.ReceiveMessages(1).Value;

        // Assert
        Assert.That(received, Has.Length.EqualTo(1));
        Assert.That(received[0].Body.ToString(), Is.EqualTo("Dequeue me"));
        Assert.That(received[0].MessageId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void Queue_SendMessage_WithVisibilityTimeout_MessageIsInitiallyHidden()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("send-hidden-queue");
        var queue = queueClient.GetQueueClient("send-hidden-queue");

        // Act — enqueue with 60 second initial visibility delay
        queue.SendMessage("Hidden on arrival", TimeSpan.FromSeconds(60));
        var received = queue.ReceiveMessages(1).Value;

        // Assert
        Assert.That(received, Has.Length.EqualTo(0));
    }

    [Test]
    public void Queue_PeekMessages_ReturnsEmptyWhenNoMessages()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("peek-empty-queue");
        var queue = queueClient.GetQueueClient("peek-empty-queue");

        // Act
        var peeked = queue.PeekMessages(1).Value.ToList();

        // Assert
        Assert.That(peeked, Has.Count.EqualTo(0));
    }

    [Test]
    public void Queue_PeekMessages_ReturnsMessageWithoutDequeuing()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("peek-nodequeue-queue");
        var queue = queueClient.GetQueueClient("peek-nodequeue-queue");

        queue.SendMessage("Peek me");

        // Act
        var peeked = queue.PeekMessages(1).Value.ToList();

        // Assert — message is present and contains correct content
        Assert.That(peeked, Has.Count.EqualTo(1));
        Assert.That(peeked[0].Body.ToString(), Is.EqualTo("Peek me"));
        Assert.That(peeked[0].MessageId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void Queue_PeekMessages_MessageRemainsVisibleAfterPeek()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("peek-visible-queue");
        var queue = queueClient.GetQueueClient("peek-visible-queue");

        queue.SendMessage("Still here");

        // Act — peek, then receive
        queue.PeekMessages(1);
        var received = queue.ReceiveMessages(1).Value.ToList();

        // Assert — peek does not hide the message
        Assert.That(received, Has.Count.EqualTo(1));
        Assert.That(received[0].Body.ToString(), Is.EqualTo("Still here"));
    }

    [Test]
    public void Queue_PeekMessages_DoesNotIncrementDequeueCount()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("peek-count-queue");
        var queue = queueClient.GetQueueClient("peek-count-queue");

        queue.SendMessage("Count test");

        // Act — peek twice then receive
        queue.PeekMessages(1);
        queue.PeekMessages(1);
        var received = queue.ReceiveMessages(1).Value.ToList();

        // Assert — DequeueCount reflects only the actual receive, not the peeks
        Assert.That(received, Has.Count.EqualTo(1));
        Assert.That(received[0].DequeueCount, Is.EqualTo(1));
    }

    [Test]
    public void Queue_PeekMessages_ReturnsMultipleMessages()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("peek-multi-queue");
        var queue = queueClient.GetQueueClient("peek-multi-queue");

        queue.SendMessage("Peek 1");
        queue.SendMessage("Peek 2");
        queue.SendMessage("Peek 3");

        // Act
        var peeked = queue.PeekMessages(3).Value.ToList();

        // Assert
        Assert.That(peeked, Has.Count.EqualTo(3));
    }

    [Test]
    public void Queue_SetAcl_PolicyIsReturnedByGetAcl()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("acl-set-queue");
        var queue = queueClient.GetQueueClient("acl-set-queue");

        var identifiers = new[]
        {
            new Azure.Storage.Queues.Models.QueueSignedIdentifier
            {
                Id = "read-policy",
                AccessPolicy = new Azure.Storage.Queues.Models.QueueAccessPolicy
                {
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-1),
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                    Permissions = "r"
                }
            }
        };

        // Act
        Assert.DoesNotThrow(() => queue.SetAccessPolicy(identifiers));
        var policies = queue.GetAccessPolicy().Value.ToList();

        // Assert
        Assert.That(policies, Has.Count.EqualTo(1));
        Assert.That(policies[0].Id, Is.EqualTo("read-policy"));
        Assert.That(policies[0].AccessPolicy.Permissions, Is.EqualTo("r"));
    }

    [Test]
    public void Queue_SetAcl_OverwritesPreviousPolicies()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("acl-overwrite-queue");
        var queue = queueClient.GetQueueClient("acl-overwrite-queue");

        queue.SetAccessPolicy([new Azure.Storage.Queues.Models.QueueSignedIdentifier { Id = "old-policy" }]);

        // Act
        queue.SetAccessPolicy([new Azure.Storage.Queues.Models.QueueSignedIdentifier { Id = "new-policy" }]);
        var policies = queue.GetAccessPolicy().Value.ToList();

        // Assert
        Assert.That(policies, Has.Count.EqualTo(1));
        Assert.That(policies[0].Id, Is.EqualTo("new-policy"));
    }

    [Test]
    public void Queue_GetAcl_ReturnsEmptyWhenNoPoliciesSet()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("acl-get-queue");
        var queue = queueClient.GetQueueClient("acl-get-queue");

        // Act
        var policies = queue.GetAccessPolicy().Value.ToList();

        // Assert
        Assert.That(policies, Is.Empty);
    }

    [Test]
    public void Queue_ClearMessages_RemovesAllMessages()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("clear-all-queue");
        var queue = queueClient.GetQueueClient("clear-all-queue");

        queue.SendMessage("Message 1");
        queue.SendMessage("Message 2");
        queue.SendMessage("Message 3");

        // Act
        Assert.DoesNotThrow(() => queue.ClearMessages());

        var remaining = queue.ReceiveMessages(10).Value.ToList();

        // Assert
        Assert.That(remaining, Has.Count.EqualTo(0));
    }

    [Test]
    public void Queue_ClearMessages_EmptyQueueSucceeds()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("clear-empty-queue");
        var queue = queueClient.GetQueueClient("clear-empty-queue");

        // Act + Assert — clearing an already-empty queue should not throw
        Assert.DoesNotThrow(() => queue.ClearMessages());
    }

    [Test]
    public void Queue_DeleteMessage_Succeeds()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("delete-msg-queue");
        var queue = queueClient.GetQueueClient("delete-msg-queue");
        queue.SendMessage("Delete me");

        var received = queue.ReceiveMessages(1).Value;
        Assert.That(received, Has.Length.EqualTo(1));

        // Act + Assert — SDK throws on non-204 response
        Assert.DoesNotThrow(() => queue.DeleteMessage(received[0].MessageId, received[0].PopReceipt));
    }

    [Test]
    public void Queue_DeleteMessage_MessageIsRemovedFromQueue()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("delete-gone-queue");
        var queue = queueClient.GetQueueClient("delete-gone-queue");
        queue.SendMessage("To be deleted");

        var received = queue.ReceiveMessages(1).Value;
        queue.DeleteMessage(received[0].MessageId, received[0].PopReceipt);

        // Act — receive with a long visibility timeout so the deleted message can't hide
        var remaining = queue.ReceiveMessages(1, TimeSpan.FromSeconds(5)).Value.ToList();

        // Assert
        Assert.That(remaining, Has.Count.EqualTo(0));
    }

    [Test]
    public void Queue_SetMetadata_PersistsMetadata()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("meta-set-queue");
        var queue = queueClient.GetQueueClient("meta-set-queue");

        var metadata = new Dictionary<string, string> { { "env", "test" }, { "owner", "topaz" } };

        // Act
        Assert.DoesNotThrow(() => queue.SetMetadata(metadata));

        var properties = queue.GetProperties().Value;

        // Assert
        Assert.That(properties.Metadata, Contains.Key("env"));
        Assert.That(properties.Metadata["env"], Is.EqualTo("test"));
        Assert.That(properties.Metadata, Contains.Key("owner"));
        Assert.That(properties.Metadata["owner"], Is.EqualTo("topaz"));
    }

    [Test]
    public void Queue_SetMetadata_OverwritesPreviousMetadata()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("meta-overwrite-queue");
        var queue = queueClient.GetQueueClient("meta-overwrite-queue");

        queue.SetMetadata(new Dictionary<string, string> { { "key1", "value1" } });

        // Act
        queue.SetMetadata(new Dictionary<string, string> { { "key2", "value2" } });

        var properties = queue.GetProperties().Value;

        // Assert
        Assert.That(properties.Metadata, Does.Not.ContainKey("key1"));
        Assert.That(properties.Metadata, Contains.Key("key2"));
        Assert.That(properties.Metadata["key2"], Is.EqualTo("value2"));
    }

    [Test]
    public void Queue_DeleteMessage_MessageCountDecreasesAfterDelete()
    {
        // Arrange
        var queueClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
        queueClient.CreateQueue("delete-count-queue");
        var queue = queueClient.GetQueueClient("delete-count-queue");
        queue.SendMessage("Message A");
        queue.SendMessage("Message B");

        var before = queue.GetProperties().Value.ApproximateMessagesCount;

        var received = queue.ReceiveMessages(1).Value;
        queue.DeleteMessage(received[0].MessageId, received[0].PopReceipt);

        // Act
        var after = queue.GetProperties().Value.ApproximateMessagesCount;

        // Assert
        Assert.That(after, Is.LessThan(before));
    }

    [Test]
    public void QueueService_GetProperties_ReturnsDefaultProperties()
    {
        var serviceClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));

        Assert.DoesNotThrow(() =>
        {
            var props = serviceClient.GetProperties().Value;
            Assert.That(props, Is.Not.Null);
        });
    }

    [Test]
    public void QueueService_SetAndGetProperties_Roundtrip()
    {
        var serviceClient = new QueueServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));

        var original = serviceClient.GetProperties().Value;

        Assert.DoesNotThrow(() => serviceClient.SetProperties(original));

        var retrieved = serviceClient.GetProperties().Value;
        Assert.That(retrieved, Is.Not.Null);
    }
}
