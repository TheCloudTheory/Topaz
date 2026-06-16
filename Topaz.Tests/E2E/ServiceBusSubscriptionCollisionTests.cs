using Azure.Messaging.ServiceBus.Administration;
using Topaz.CLI;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ServiceBusSubscriptionCollisionTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("CC2A1B3D-5678-4E6F-A0B1-8C9D2E3F4A5B");

    private const string SubscriptionName = "sub-collision-test";
    private const string ResourceGroupName = "rg-sb-collision-test";
    private const string NamespaceName = "sb-collision-test";
    private const string TopicA = "topic-alpha";
    private const string TopicB = "topic-beta";
    private const string SharedSubscriptionName = "shared-sub";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);

        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        await Program.RunAsync(["servicebus", "namespace", "delete", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["servicebus", "namespace", "create", "--name", NamespaceName, "--resource-group", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        var client = new ServiceBusAdministrationClient(TopazResourceHelpers.GetServiceBusConnectionStringForManagement(NamespaceName));
        await client.CreateTopicAsync(TopicA);
        await client.CreateTopicAsync(TopicB);
    }

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
    }

    private ServiceBusAdministrationClient CreateClient() =>
        new(TopazResourceHelpers.GetServiceBusConnectionStringForManagement(NamespaceName));

    [Test]
    public async Task CreateSameNamedSubscriptionUnderDifferentTopics_BothAreIndependentlyRetrievable()
    {
        // Arrange
        var client = CreateClient();

        // Act — create same subscription name under two different topics
        var subA = await client.CreateSubscriptionAsync(new CreateSubscriptionOptions(TopicA, SharedSubscriptionName));
        var subB = await client.CreateSubscriptionAsync(new CreateSubscriptionOptions(TopicB, SharedSubscriptionName));

        // Assert both exist independently
        Assert.Multiple(() =>
        {
            Assert.That(subA.Value.SubscriptionName, Is.EqualTo(SharedSubscriptionName));
            Assert.That(subA.Value.TopicName, Is.EqualTo(TopicA));
            Assert.That(subB.Value.SubscriptionName, Is.EqualTo(SharedSubscriptionName));
            Assert.That(subB.Value.TopicName, Is.EqualTo(TopicB));
        });

        // Act — retrieve each independently
        var gotA = await client.GetSubscriptionAsync(TopicA, SharedSubscriptionName);
        var gotB = await client.GetSubscriptionAsync(TopicB, SharedSubscriptionName);

        Assert.Multiple(() =>
        {
            Assert.That(gotA.Value.TopicName, Is.EqualTo(TopicA));
            Assert.That(gotB.Value.TopicName, Is.EqualTo(TopicB));
        });
    }

    [Test]
    public async Task ListSubscriptions_EachTopicReturnsOnlyItsOwnSubscription()
    {
        // Arrange
        var client = CreateClient();
        await client.CreateSubscriptionAsync(new CreateSubscriptionOptions(TopicA, SharedSubscriptionName));
        await client.CreateSubscriptionAsync(new CreateSubscriptionOptions(TopicB, SharedSubscriptionName));

        // Act
        var subsA = new List<SubscriptionProperties>();
        await foreach (var s in client.GetSubscriptionsAsync(TopicA))
            subsA.Add(s);

        var subsB = new List<SubscriptionProperties>();
        await foreach (var s in client.GetSubscriptionsAsync(TopicB))
            subsB.Add(s);

        // Assert each topic has exactly one subscription
        Assert.Multiple(() =>
        {
            Assert.That(subsA, Has.Count.EqualTo(1));
            Assert.That(subsA[0].TopicName, Is.EqualTo(TopicA));
            Assert.That(subsB, Has.Count.EqualTo(1));
            Assert.That(subsB[0].TopicName, Is.EqualTo(TopicB));
        });
    }

    [Test]
    public async Task DeleteSubscriptionUnderOneTopicDoesNotAffectOtherTopic()
    {
        // Arrange
        var client = CreateClient();
        await client.CreateSubscriptionAsync(new CreateSubscriptionOptions(TopicA, SharedSubscriptionName));
        await client.CreateSubscriptionAsync(new CreateSubscriptionOptions(TopicB, SharedSubscriptionName));

        // Act — delete from TopicA only
        await client.DeleteSubscriptionAsync(TopicA, SharedSubscriptionName);

        // Assert TopicB subscription is unaffected
        var gotB = await client.GetSubscriptionAsync(TopicB, SharedSubscriptionName);
        Assert.That(gotB.Value.SubscriptionName, Is.EqualTo(SharedSubscriptionName));

        // Assert TopicA subscription is gone
        Assert.That(
            async () => await client.GetSubscriptionAsync(TopicA, SharedSubscriptionName),
            Throws.Exception);
    }
}
