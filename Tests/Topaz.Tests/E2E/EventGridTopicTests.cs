using Azure;
using Azure.Core;
using Azure.Messaging.EventGrid;
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Value.Data.Name, Is.EqualTo(TopicName));
            Assert.That(result.Value.Data.Location, Is.EqualTo(new AzureLocation("westeurope")));
        }
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(keys.Value.Key1, Is.Not.Null.And.Not.Empty);
            Assert.That(keys.Value.Key2, Is.Not.Null.And.Not.Empty);
        }
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
    
    [Test]
    public async Task EventGridTopicSubscription_AllOperationsAreWorking()
    {
        const string eventSubscriptionName = "test-subscription";
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var topics = resourceGroup.Value.GetEventGridTopics();
        var data = new EventGridTopicData(new AzureLocation("westeurope"));

        var topic = await topics.CreateOrUpdateAsync(WaitUntil.Completed, TopicName, data);
        var eventSubscription = await topic.Value.GetTopicEventSubscriptions().CreateOrUpdateAsync(WaitUntil.Completed, eventSubscriptionName, new EventGridSubscriptionData
        {
            Destination = new WebHookEventSubscriptionDestination
            {
                Endpoint = new Uri("https://example.com"),
                MaxEventsPerBatch = 10,
                PreferredBatchSizeInKilobytes = 64,
                DeliveryAttributeMappings =
                {
                    new StaticDeliveryAttributeMapping
                    {
                        IsSecret = true,
                        Name = "TestName",
                        Value = "TestValue"
                    },
                    new DynamicDeliveryAttributeMapping
                    {
                        Name = "TestName",
                        SourceField = "$.some.path"
                    }
                }
            }
        });
        
        using (Assert.EnterMultipleScope())
        {
            Assert.That(eventSubscription.Value.Data.Name, Is.EqualTo(eventSubscriptionName));
            Assert.That(((WebHookEventSubscriptionDestination)eventSubscription.Value.Data.Destination).Endpoint, Is.EqualTo(new Uri("https://example.com")));
        }
        
        var returnedEventSubscription = await topic.Value.GetTopicEventSubscriptions().GetAsync(eventSubscriptionName);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(returnedEventSubscription.Value.Data.Name, Is.EqualTo(eventSubscriptionName));
            Assert.That(((WebHookEventSubscriptionDestination)eventSubscription.Value.Data.Destination).Endpoint, Is.EqualTo(new Uri("https://example.com")));
        }
        
        var uri = await returnedEventSubscription.Value.GetFullUriAsync(CancellationToken.None);
        var attributes = await returnedEventSubscription.Value.GetDeliveryAttributesAsync(CancellationToken.None).ToArrayAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(uri.Value.Endpoint, Is.EqualTo(new Uri("https://example.com")));
            Assert.That(attributes, Has.Length.EqualTo(2));
        }

        _ = returnedEventSubscription.Value.DeleteAsync(WaitUntil.Completed);
    }

    [Test]
    public async Task EventGridTopicSubscription_CanSendEventToTopicUrl()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var topics = resourceGroup.Value.GetEventGridTopics();
        var data = new EventGridTopicData(new AzureLocation("westeurope"));

        var topic = await topics.CreateOrUpdateAsync(WaitUntil.Completed, TopicName, data);
        var endpoint = topic.Value.Data.Endpoint;

        var client = new EventGridPublisherClient(
            endpoint,
            new AzureLocalCredential(Globals.GlobalAdminId));
        
        var eventGridEvent =
            new EventGridEvent(
                "ExampleEventSubject",
                "Example.EventType",
                "1.0",
                "This is the event data");
        
        await client.SendEventAsync(eventGridEvent);
    }
    
    [Test]
    public async Task EventGridTopicSubscription_IfMoreThat5000EventsAreSent_ItShouldFail()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var topics = resourceGroup.Value.GetEventGridTopics();
        var data = new EventGridTopicData(new AzureLocation("westeurope"));

        var topic = await topics.CreateOrUpdateAsync(WaitUntil.Completed, TopicName, data);
        var endpoint = topic.Value.Data.Endpoint;

        var client = new EventGridPublisherClient(
            endpoint,
            new AzureLocalCredential(Globals.GlobalAdminId));
        
        var events = new List<EventGridEvent>();
        for (var i = 0; i <= 5000; i++)
        {
            events.Add(new EventGridEvent(
                "ExampleEventSubject",
                "Example.EventType",
                "1.0",
                "This is the event data"));
        }
        
        Assert.ThrowsAsync<RequestFailedException>(() => client.SendEventsAsync(events), "A batch can contain a maximum of 5,000 events.");
    }
    
    [Test]
    public async Task EventGridTopicSubscription_IfPayloadExceedsOneMegabyte_ItShouldFail()
    {
        var armClient = new ArmClient(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var topics = resourceGroup.Value.GetEventGridTopics();
        var topicData = new EventGridTopicData(new AzureLocation("westeurope"));

        var topic = await topics.CreateOrUpdateAsync(WaitUntil.Completed, TopicName, topicData);
        var endpoint = topic.Value.Data.Endpoint;

        var client = new EventGridPublisherClient(
            endpoint,
            new AzureLocalCredential(Globals.GlobalAdminId));
        
        var data = new string('a', 1024 * 1024);
        var @event = new EventGridEvent(
            "ExampleEventSubject",
            "Example.EventType",
            "1.0",
            data);
        
        Assert.ThrowsAsync<RequestFailedException>(() => client.SendEventAsync(@event), "A batch can contain a maximum of 1 MB.");
    }
}
