using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class EventGridTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("4A1B2C3D-EEEE-4F5A-8BB1-3CFE44084F82");
    private const string ResourceGroupName = "test";
    private const string NamespaceName = "test-namespace";

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
            "sub-test"
        ]);

        await Program.RunAsync([
            "group",
            "delete",
            "--name",
            ResourceGroupName
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
            "eventgrid",
            "namespace",
            "delete",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "eventgrid",
            "namespace",
            "create",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
    }

    [Test]
    public void EventGridTests_WhenNewNamespaceIsRequested_ItShouldBeCreated()
    {
        var namespacePath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".event-grid-namespace", NamespaceName, "metadata.json");

        Assert.That(File.Exists(namespacePath), Is.True);
    }

    [Test]
    public async Task EventGridTests_WhenExistingNamespaceIsDeleted_ItShouldBeDeleted()
    {
        var namespacePath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".event-grid-namespace", NamespaceName, "metadata.json");

        var code = await Program.RunAsync([
            "eventgrid",
            "namespace",
            "delete",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(File.Exists(namespacePath), Is.False);
            Assert.That(code, Is.Zero);
        }
    }

    [Test]
    public async Task EventGridTests_WhenExistingNamespaceIsRequested_ItShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "namespace",
            "show",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenNamespacesInResourceGroupAreListed_TheyShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "namespace",
            "list-resource-group",
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenNamespacesInSubscriptionAreListed_TheyShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "namespace",
            "list-subscription",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenExistingNamespaceIsUpdated_ItShouldBeUpdated()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "namespace",
            "update",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString(),
            "--sku-name",
            "Standard"
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenNamespaceKeysAreListed_TheyShouldBeReturned()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "namespace",
            "list-keys",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task EventGridTests_WhenNamespaceKeyIsRegenerated_ItShouldSucceed()
    {
        var code = await Program.RunAsync([
            "eventgrid",
            "namespace",
            "regenerate-key",
            "--name",
            NamespaceName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString(),
            "--key-name",
            "key1"
        ]);

        Assert.That(code, Is.Zero);
    }
}
