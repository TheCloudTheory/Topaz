using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.EventGrid;
using Azure.ResourceManager.EventGrid.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class EventGridTopicTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("4B2C3D4E-5F6A-7B8C-9D0E-1F2A3B4C5D6E");

    private const string SubscriptionName = "sub-eventgrid-topic-test";
    private const string ResourceGroupName = "test-eventgrid-topic";
    private const string TopicName = "test-topic";

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
    public async Task EventGridTopic_Create_ReturnsCreated()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var topics = resourceGroup.Value.GetEventGridTopics();
        var data = new EventGridTopicData(new AzureLocation("westeurope"));

        var result = await topics.CreateOrUpdateAsync(WaitUntil.Completed, TopicName, data);

        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Data.Name, Is.EqualTo(TopicName));
            Assert.That(result.Value.Data.Location, Is.EqualTo(new AzureLocation("westeurope")));
        });
    }

    [Test]
    public async Task EventGridTopic_Get_ReturnsTopic()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var topics = resourceGroup.Value.GetEventGridTopics();
        await topics.CreateOrUpdateAsync(WaitUntil.Completed, TopicName, new EventGridTopicData(new AzureLocation("westeurope")));

        var result = await topics.GetAsync(TopicName);

        Assert.That(result.Value.Data.Name, Is.EqualTo(TopicName));
    }

    [Test]
    public async Task EventGridTopic_Get_NotFound_ThrowsRequestFailedException()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var topics = resourceGroup.Value.GetEventGridTopics();

        Assert.ThrowsAsync<RequestFailedException>(async () => await topics.GetAsync("does-not-exist"));
    }

    [Test]
    public async Task EventGridTopic_Update_ModifiesTags()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var topics = resourceGroup.Value.GetEventGridTopics();
        var created = await topics.CreateOrUpdateAsync(WaitUntil.Completed, TopicName, new EventGridTopicData(new AzureLocation("westeurope")));

        var patch = new EventGridTopicPatch();
        patch.Tags.Add("env", "test");
        await created.Value.UpdateAsync(WaitUntil.Completed, patch);

        var updated = await topics.GetAsync(TopicName);

        Assert.That(updated.Value.Data.Tags, Contains.Key("env"));
    }

    [Test]
    public async Task EventGridTopic_Delete_RemovesTopic()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var topics = resourceGroup.Value.GetEventGridTopics();
        var created = await topics.CreateOrUpdateAsync(WaitUntil.Completed, TopicName, new EventGridTopicData(new AzureLocation("westeurope")));

        await created.Value.DeleteAsync(WaitUntil.Completed);

        Assert.ThrowsAsync<RequestFailedException>(async () => await topics.GetAsync(TopicName));
    }

    [Test]
    public async Task EventGridTopic_ListByResourceGroup_ContainsCreatedTopic()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var topics = resourceGroup.Value.GetEventGridTopics();
        await topics.CreateOrUpdateAsync(WaitUntil.Completed, TopicName, new EventGridTopicData(new AzureLocation("westeurope")));

        var list = topics.GetAllAsync();
        var names = new List<string>();
        await foreach (var topic in list)
        {
            names.Add(topic.Data.Name);
        }

        Assert.That(names, Contains.Item(TopicName));
    }

    [Test]
    public async Task EventGridTopic_ListBySubscription_ContainsCreatedTopic()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetEventGridTopics().CreateOrUpdateAsync(WaitUntil.Completed, TopicName, new EventGridTopicData(new AzureLocation("westeurope")));

        var names = new List<string>();
        await foreach (var topic in subscription.GetEventGridTopicsAsync())
        {
            names.Add(topic.Data.Name);
        }

        Assert.That(names, Contains.Item(TopicName));
    }

    [Test]
    public async Task EventGridTopic_ListSharedAccessKeys_ReturnsKeys()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var created = await resourceGroup.Value.GetEventGridTopics().CreateOrUpdateAsync(WaitUntil.Completed, TopicName, new EventGridTopicData(new AzureLocation("westeurope")));

        var keys = await created.Value.GetSharedAccessKeysAsync();

        Assert.Multiple(() =>
        {
            Assert.That(keys.Value.Key1, Is.Not.Null.And.Not.Empty);
            Assert.That(keys.Value.Key2, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task EventGridTopic_RegenerateKey_ReturnsNewKey()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var created = await resourceGroup.Value.GetEventGridTopics().CreateOrUpdateAsync(WaitUntil.Completed, TopicName, new EventGridTopicData(new AzureLocation("westeurope")));

        var originalKeys = await created.Value.GetSharedAccessKeysAsync();
        var originalKey1 = originalKeys.Value.Key1;

        var regenerated = await created.Value.RegenerateKeyAsync(WaitUntil.Completed, new TopicRegenerateKeyContent("key1"));

        Assert.That(regenerated.Value.Key1, Is.Not.EqualTo(originalKey1));
    }
}
