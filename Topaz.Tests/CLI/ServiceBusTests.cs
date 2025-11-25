using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class ServiceBusTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("2F643E9E-DDDD-42D2-8AA0-2BFE33073E71");
    private static readonly string ResourceGroupName = "test";
    private static readonly string NamespaceName = "test";
    private static readonly string QueueName = "queue-test";
    
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
            "sub-test"
        ]);

        await Program.Main([
            "group",
            "delete",
            "--name",
            ResourceGroupName
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
        
        await Program.Main([
            "servicebus",
            "queue",
            "create",
            "--queue-name",
            QueueName,
            "--namespace-name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
    }
    
    [Test]
    public void ServiceBusTests_WhenNewNamespacesIsRequested_ItShouldBeCreated()
    {
        var namespacePath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".service-bus", NamespaceName, "metadata.json");

        Assert.That(File.Exists(namespacePath), Is.True);
    }
    
    [Test]
    public async Task ServiceBusTests_WhenExistingNamespaceIsDeleted_ItShouldBeDeleted()
    {
        var namespacePath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".service-bus", NamespaceName, "metadata.json");

        var code = await Program.Main([
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
        
        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(namespacePath), Is.False);
            Assert.That(code, Is.EqualTo(0));
        });
    }
    
    [Test]
    public void ServiceBusTests_WhenNewQueueIsRequested_ItShouldBeCreated()
    {
        var queuePath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".service-bus", NamespaceName, "queues", QueueName, "metadata.json");

        Assert.That(File.Exists(queuePath), Is.True);
    }
    
    [Test]
    public async Task ServiceBusTests_WhenNewQueueIsDeleted_ItShouldBeDeleted()
    {
        var queuePath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".service-bus", NamespaceName, "queues", QueueName, "metadata.json");
        
        var code = await Program.Main([
            "servicebus",
            "queue",
            "delete",
            "--queue-name",
            QueueName,
            "--namespace-name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(queuePath), Is.False);
            Assert.That(code, Is.EqualTo(0));
        });
    }
}