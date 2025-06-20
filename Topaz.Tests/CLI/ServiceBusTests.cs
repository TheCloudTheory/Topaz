using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class ServiceBusTests
{
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    private static readonly string ResourceGroupName = "test";
    private static readonly string NamespaceName = "test";
    
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
            NamespaceName
        ]);
        
        await Program.Main([
            "servicebus",
            "namespace",
            "create",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName
        ]);
    }
    
    [Test]
    public void StorageAccountTests_WhenNewStorageAccountIsRequested_ItShouldBeCreated()
    {
        var namespacePath = Path.Combine(".topaz", ".service-bus", NamespaceName, "metadata.json");

        Assert.That(File.Exists(namespacePath), Is.True);
    }
    
    [Test]
    public async Task StorageAccountTests_WhenExistingStorageAccountIsDeleted_ItShouldBeDeleted()
    {
        var namespacePath = Path.Combine(".topaz", ".service-bus", NamespaceName, "metadata.json");

        var code = await Program.Main([
            "servicebus",
            "namespace",
            "delete",
            "--name",
            NamespaceName
        ]);
        
        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(namespacePath), Is.False);
            Assert.That(code, Is.EqualTo(0));
        });
    }
}