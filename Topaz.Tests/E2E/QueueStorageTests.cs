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
}
