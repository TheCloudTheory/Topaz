using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.EventGrid;
using Azure.ResourceManager.EventGrid.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class EventGridNamespaceTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("3A1B2C3D-4E5F-6A7B-8C9D-0E1F2A3B4C5D");

    private const string SubscriptionName = "sub-eventgrid-test";
    private const string ResourceGroupName = "test-eventgrid";
    private const string NamespaceName = "test-namespace";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);

        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
    }

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
    }

    [Test]
    public async Task EventGridNamespace_Create_ReturnsCreated()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var namespaces = resourceGroup.Value.GetEventGridNamespaces();
        var data = new EventGridNamespaceData(new AzureLocation("westeurope"));

        var result = await namespaces.CreateOrUpdateAsync(WaitUntil.Completed, NamespaceName, data);

        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Data.Name, Is.EqualTo(NamespaceName));
            Assert.That(result.Value.Data.Location, Is.EqualTo(new AzureLocation("westeurope")));
        });
    }

    [Test]
    public async Task EventGridNamespace_Get_ReturnsNamespace()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var namespaces = resourceGroup.Value.GetEventGridNamespaces();
        await namespaces.CreateOrUpdateAsync(WaitUntil.Completed, NamespaceName, new EventGridNamespaceData(new AzureLocation("westeurope")));

        var result = await namespaces.GetAsync(NamespaceName);

        Assert.That(result.Value.Data.Name, Is.EqualTo(NamespaceName));
    }

    [Test]
    public async Task EventGridNamespace_Get_NotFound_ThrowsRequestFailedException()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var namespaces = resourceGroup.Value.GetEventGridNamespaces();

        Assert.ThrowsAsync<RequestFailedException>(async () => await namespaces.GetAsync("does-not-exist"));
    }

    [Test]
    public async Task EventGridNamespace_Update_ModifiesTags()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var namespaces = resourceGroup.Value.GetEventGridNamespaces();
        var created = await namespaces.CreateOrUpdateAsync(WaitUntil.Completed, NamespaceName, new EventGridNamespaceData(new AzureLocation("westeurope")));

        var patch = new EventGridNamespacePatch();
        patch.Tags.Add("env", "test");
        var updated = await created.Value.UpdateAsync(WaitUntil.Completed, patch);

        Assert.That(updated.Value.Data.Tags, Contains.Key("env"));
    }

    [Test]
    public async Task EventGridNamespace_Delete_RemovesNamespace()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var namespaces = resourceGroup.Value.GetEventGridNamespaces();
        var created = await namespaces.CreateOrUpdateAsync(WaitUntil.Completed, NamespaceName, new EventGridNamespaceData(new AzureLocation("westeurope")));

        await created.Value.DeleteAsync(WaitUntil.Completed);

        Assert.ThrowsAsync<RequestFailedException>(async () => await namespaces.GetAsync(NamespaceName));
    }

    [Test]
    public async Task EventGridNamespace_ListByResourceGroup_ContainsCreatedNamespace()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var namespaces = resourceGroup.Value.GetEventGridNamespaces();
        await namespaces.CreateOrUpdateAsync(WaitUntil.Completed, NamespaceName, new EventGridNamespaceData(new AzureLocation("westeurope")));

        var list = namespaces.GetAllAsync();
        var names = new List<string>();
        await foreach (var ns in list)
        {
            names.Add(ns.Data.Name);
        }

        Assert.That(names, Contains.Item(NamespaceName));
    }

    [Test]
    public async Task EventGridNamespace_ListBySubscription_ContainsCreatedNamespace()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetEventGridNamespaces().CreateOrUpdateAsync(WaitUntil.Completed, NamespaceName, new EventGridNamespaceData(new AzureLocation("westeurope")));

        var names = new List<string>();
        await foreach (var ns in subscription.GetEventGridNamespacesAsync())
        {
            names.Add(ns.Data.Name);
        }

        Assert.That(names, Contains.Item(NamespaceName));
    }

    [Test]
    public async Task EventGridNamespace_ListSharedAccessKeys_ReturnsKeys()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var created = await resourceGroup.Value.GetEventGridNamespaces().CreateOrUpdateAsync(WaitUntil.Completed, NamespaceName, new EventGridNamespaceData(new AzureLocation("westeurope")));

        var keys = await created.Value.GetSharedAccessKeysAsync();

        Assert.Multiple(() =>
        {
            Assert.That(keys.Value.Key1, Is.Not.Null.And.Not.Empty);
            Assert.That(keys.Value.Key2, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task EventGridNamespace_RegenerateKey_ReturnsNewKey()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var created = await resourceGroup.Value.GetEventGridNamespaces().CreateOrUpdateAsync(WaitUntil.Completed, NamespaceName, new EventGridNamespaceData(new AzureLocation("westeurope")));

        var originalKeys = await created.Value.GetSharedAccessKeysAsync();
        var originalKey1 = originalKeys.Value.Key1;

        var regenerated = await created.Value.RegenerateKeyAsync(WaitUntil.Completed, new NamespaceRegenerateKeyContent("key1"));

        Assert.That(regenerated.Value.Key1, Is.Not.EqualTo(originalKey1));
    }
}
