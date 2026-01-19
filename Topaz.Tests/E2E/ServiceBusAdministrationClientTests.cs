using Azure.Messaging.ServiceBus.Administration;
using Topaz.CLI;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ServiceBusAdministrationClientTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("AD67D396-BEF3-40AB-8B50-68FC37B0D72D");
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string NamespaceName = "sb-test";
    private const string QueueName = "sb-test-queue";
    private const string TopicName = "sb-test-topic";
    private const string TopicWithSubscriptionName = "sb-test-topic2";
    private const string TopicSubscriptionName = "subscription1";
    
    [SetUp]
    public async Task SetUp()
    {
        await Program.Main(
        [
            "subscription",
            "delete",
            "--id",
            SubscriptionId.ToString()
        ]);
        
        await Program.Main(
        [
            "subscription",
            "create",
            "--id",
            SubscriptionId.ToString(),
            "--name",
            SubscriptionName
        ]);

        await Program.Main([
            "group",
            "delete",
            "--name",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.Main([
            "group",
            "create",
            "--name",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        await Program.Main([
            "servicebus",
            "namespace",
            "delete",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        await Program.Main([
            "servicebus",
            "namespace",
            "create",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
    }
    
    [Test]
    public async Task ServiceBusAdministrationClientTests_WhenQueueIsCreatedUsingAdministrationClient_ItShouldBeAvailable()
    {
        // Arrange
        var client =
            new ServiceBusAdministrationClient(TopazResourceHelpers.GetServiceBusConnectionStringForManagement(NamespaceName));
        
        // Act
        _ = await client.CreateQueueAsync(QueueName);
        var queue = await client.GetQueueAsync(QueueName);
        
        // Assert
        Assert.That(queue.Value.Name, Is.EqualTo(QueueName));
        
        // Cleanup
        await client.DeleteQueueAsync(QueueName);
    }
    
    [Test]
    public async Task ServiceBusAdministrationClientTests_WhenTopicIsCreatedUsingAdministrationClient_ItShouldBeAvailable()
    {
        // Arrange
        var client =
            new ServiceBusAdministrationClient(TopazResourceHelpers.GetServiceBusConnectionStringForManagement(NamespaceName));
        
        // Act
        _ = await client.CreateTopicAsync(TopicName);
        var queue = await client.GetTopicAsync(TopicName);
        
        // Assert
        Assert.That(queue.Value.Name, Is.EqualTo(TopicName));
        
        // Cleanup
        await client.DeleteTopicAsync(TopicName);
    }
    
    [Test]
    public async Task ServiceBusAdministrationClientTests_WhenSubscriptionIsCreatedUsingAdministrationClient_ItShouldBeAvailable()
    {
        // Arrange
        var client =
            new ServiceBusAdministrationClient(TopazResourceHelpers.GetServiceBusConnectionStringForManagement(NamespaceName));
        
        // Act
        _ = await client.CreateTopicAsync(TopicWithSubscriptionName);
        _ = await client.CreateSubscriptionAsync(new CreateSubscriptionOptions(TopicWithSubscriptionName, TopicSubscriptionName));
        var subscription = await client.GetSubscriptionAsync(TopicWithSubscriptionName, TopicSubscriptionName);
        
        // Assert
        Assert.That(subscription.Value.TopicName, Is.EqualTo(TopicWithSubscriptionName));
        
        // Cleanup
        await client.DeleteSubscriptionAsync(TopicWithSubscriptionName, TopicSubscriptionName);
        await client.DeleteTopicAsync(TopicWithSubscriptionName);
    }
}