using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ServiceBus;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ServiceBusServiceTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("AD67D396-BEF3-40AB-8B50-68FC37B0D72D");
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string NamespaceName = "sb-test";
    private const string QueueName = "sb-test-queue";
    private const string TopicName = "sb-test-topic";
    
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
    }

    [Test]
    public async Task ServiceBusServiceTests_WhenNamespaceIsCreatedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var data = new ServiceBusNamespaceData(AzureLocation.WestEurope);
        
        // Act
        _ = await resourceGroup.Value.GetServiceBusNamespaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, NamespaceName, data);
        var @namespace = await resourceGroup.Value.GetServiceBusNamespaces().GetAsync(NamespaceName);
        
        // Assert
        Assert.That(@namespace, Is.Not.Null);
        Assert.That(@namespace.Value, Is.Not.Null);
        Assert.That(@namespace.Value.Data.Name, Is.EqualTo(NamespaceName));
    }
    
    [Test]
    public async Task ServiceBusServiceTests_WhenQueueIsCreatedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var data = new ServiceBusNamespaceData(AzureLocation.WestEurope);
        
        // Act
        _ = await resourceGroup.Value.GetServiceBusNamespaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, NamespaceName, data);
        var @namespace = await resourceGroup.Value.GetServiceBusNamespaces().GetAsync(NamespaceName);
        _ = await @namespace.Value.GetServiceBusQueues().CreateOrUpdateAsync(WaitUntil.Completed, QueueName, new ServiceBusQueueData());
        var queue =  await @namespace.Value.GetServiceBusQueues().GetAsync(QueueName);
        
        // Assert
        Assert.That(queue, Is.Not.Null);
        Assert.That(queue.Value, Is.Not.Null);
        Assert.That(queue.Value.Data.Name, Is.EqualTo(QueueName));
    }
    
    [Test]
    public async Task ServiceBusServiceTests_WhenTopicIsCreatedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var data = new ServiceBusNamespaceData(AzureLocation.WestEurope);
        
        // Act
        _ = await resourceGroup.Value.GetServiceBusNamespaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, NamespaceName, data);
        var @namespace = await resourceGroup.Value.GetServiceBusNamespaces().GetAsync(NamespaceName);
        _ = await @namespace.Value.GetServiceBusTopics().CreateOrUpdateAsync(WaitUntil.Completed, TopicName, new ServiceBusTopicData());
        var topic =  await @namespace.Value.GetServiceBusTopics().GetAsync(TopicName);
        
        // Assert
        Assert.That(topic, Is.Not.Null);
        Assert.That(topic.Value, Is.Not.Null);
        Assert.That(topic.Value.Data.Name, Is.EqualTo(TopicName));
    }
    
    [Test]
    public async Task ServiceBusServiceTests_WhenListingNamespacesUsingSDK_ItShouldReturnCreatedNamespaces()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var data = new ServiceBusNamespaceData(AzureLocation.WestEurope);
        
        // Act
        _ = await resourceGroup.Value.GetServiceBusNamespaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, NamespaceName, data);
        
        var namespaces = resourceGroup.Value.GetServiceBusNamespaces().GetAllAsync();
        var namespaceList = new List<ServiceBusNamespaceResource>();
        await foreach (var ns in namespaces)
        {
            namespaceList.Add(ns);
        }
        
        // Assert
        Assert.That(namespaceList, Is.Not.Null);
        Assert.That(namespaceList, Is.Not.Empty);
        Assert.That(namespaceList.Any(ns => ns.Data.Name == NamespaceName), Is.True);
    }
    
    [Test]
    public async Task ServiceBusServiceTests_WhenListingNamespacesUsingSDKWhenThereIsNotNamespace_ItShouldReturnEmptyResult()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        
        // Act
        var namespaces = resourceGroup.Value.GetServiceBusNamespaces().GetAllAsync();
        var namespaceList = new List<ServiceBusNamespaceResource>();
        await foreach (var ns in namespaces)
        {
            namespaceList.Add(ns);
        }
        
        // Assert
        Assert.That(namespaceList, Is.Not.Null);
        Assert.That(namespaceList, Is.Empty);
    }
    
    [Test]
    public async Task ServiceBusServiceTests_WhenDeletingNamespaceUsingSDK_ItShouldBeRemoved()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var data = new ServiceBusNamespaceData(AzureLocation.WestEurope);
        
        // Act
        _ = await resourceGroup.Value.GetServiceBusNamespaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, NamespaceName, data);
        var @namespace = await resourceGroup.Value.GetServiceBusNamespaces().GetAsync(NamespaceName);
        await @namespace.Value.DeleteAsync(WaitUntil.Completed);
        
        // Assert - verify namespace is no longer in the list
        var namespaces = resourceGroup.Value.GetServiceBusNamespaces().GetAllAsync();
        var namespaceList = new List<ServiceBusNamespaceResource>();
        await foreach (var ns in namespaces)
        {
            namespaceList.Add(ns);
        }
        
        Assert.That(namespaceList.Any(ns => ns.Data.Name == NamespaceName), Is.False);
    }
    
    [Test]
    public async Task ServiceBusServiceTests_WhenListingQueuesUsingSDK_ItShouldReturnCreatedQueues()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var namespaceData = new ServiceBusNamespaceData(AzureLocation.WestEurope);
        
        // Act
        _ = await resourceGroup.Value.GetServiceBusNamespaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, NamespaceName, namespaceData);
        var @namespace = await resourceGroup.Value.GetServiceBusNamespaces().GetAsync(NamespaceName);
        
        _ = await @namespace.Value.GetServiceBusQueues()
            .CreateOrUpdateAsync(WaitUntil.Completed, QueueName, new ServiceBusQueueData());
        
        var queues = @namespace.Value.GetServiceBusQueues().GetAllAsync();
        var queueList = new List<ServiceBusQueueResource>();
        await foreach (var queue in queues)
        {
            queueList.Add(queue);
        }
        
        // Assert
        Assert.That(queueList, Is.Not.Null);
        Assert.That(queueList, Is.Not.Empty);
        Assert.That(queueList.Any(q => q.Data.Name == QueueName), Is.True);
    }
    
    [Test]
    public async Task ServiceBusServiceTests_WhenUpdatingQueueUsingSDK_ItShouldReflectChanges()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var namespaceData = new ServiceBusNamespaceData(AzureLocation.WestEurope);
        
        // Act
        _ = await resourceGroup.Value.GetServiceBusNamespaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, NamespaceName, namespaceData);
        var @namespace = await resourceGroup.Value.GetServiceBusNamespaces().GetAsync(NamespaceName);
        
        var initialQueueData = new ServiceBusQueueData { MaxDeliveryCount = 10 };
        _ = await @namespace.Value.GetServiceBusQueues()
            .CreateOrUpdateAsync(WaitUntil.Completed, QueueName, initialQueueData);
        
        var updatedQueueData = new ServiceBusQueueData { MaxDeliveryCount = 20 };
        _ = await @namespace.Value.GetServiceBusQueues()
            .CreateOrUpdateAsync(WaitUntil.Completed, QueueName, updatedQueueData);
        
        var queue = await @namespace.Value.GetServiceBusQueues().GetAsync(QueueName);
        
        // Assert
        Assert.That(queue, Is.Not.Null);
        Assert.That(queue.Value, Is.Not.Null);
        Assert.That(queue.Value.Data.MaxDeliveryCount, Is.EqualTo(20));
    }
}