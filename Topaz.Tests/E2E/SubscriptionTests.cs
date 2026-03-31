using Azure.ResourceManager;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class SubscriptionTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [Test]
    public async Task SubscriptionTests_WhenSubscriptionIsRequested_ItShouldBeAvailable()
    {
        // Arrange 
        await Program.Main(
        [
            "subscription",
            "delete",
            "--id",
            Guid.Empty.ToString()
        ]);

        await Program.Main(
        [
            "subscription",
            "create",
            "--id",
            Guid.Empty.ToString(),
            "--name",
            "sub-test"
        ]);

        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, Guid.Empty.ToString(), ArmClientOptions);

        // Act
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(subscription.Data.Id.ToString(), Is.EqualTo($"/subscriptions/{Guid.Empty}"));
            Assert.That(subscription.Data.SubscriptionId, Is.EqualTo(Guid.Empty.ToString()));
            Assert.That(subscription.Data.DisplayName, Is.EqualTo("sub-test"));
        });
    }

    [Test]
    public async Task SubscriptionTests_WhenSubscriptionTagsAreUpdated_TheyShouldShouldBeAvailable()
    {
        // Arrange 
        var subscriptionId = Guid.NewGuid().ToString();
        await Program.Main(
        [
            "subscription",
            "delete",
            "--id",
            subscriptionId
        ]);

        await Program.Main(
        [
            "subscription",
            "create",
            "--id",
            subscriptionId,
            "--name",
            "sub-test"
        ]);

        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        var armClient = new ArmClient(credentials, subscriptionId, ArmClientOptions);

        // Act
        await topaz.UpdateSubscriptionAsync(Guid.Parse(subscriptionId), "sub-test",
            tags: new Dictionary<string, string> { { "test-key", "test-value" } });
        var updatedSubscription = await armClient.GetSubscriptions().GetAsync(subscriptionId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updatedSubscription.Value.Data.Tags, Contains.Key("test-key"));
            Assert.That(updatedSubscription.Value.Data.Tags["test-key"], Is.EqualTo("test-value"));
        });
    }

    [Test]
    public async Task SubscriptionTests_WhenSubscriptionTagsValuesAreUpdated_TheyShouldShouldBeCorrect()
    {
        // Arrange 
        var subscriptionId = Guid.NewGuid().ToString();
        await Program.Main(
        [
            "subscription",
            "delete",
            "--id",
            subscriptionId
        ]);

        await Program.Main(
        [
            "subscription",
            "create",
            "--id",
            subscriptionId,
            "--name",
            "sub-test",
            "--tag",
            "test-key=test-value"
        ]);

        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        var armClient = new ArmClient(credentials, subscriptionId, ArmClientOptions);

        // Act
        await topaz.UpdateSubscriptionAsync(Guid.Parse(subscriptionId), "sub-test",
            tags: new Dictionary<string, string> { { "test-key", "test-value-updated" } });
        var updatedSubscription = await armClient.GetSubscriptions().GetAsync(subscriptionId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updatedSubscription.Value.Data.Tags, Contains.Key("test-key"));
            Assert.That(updatedSubscription.Value.Data.Tags["test-key"], Is.EqualTo("test-value-updated"));
        });
    }

    [Test]
    public async Task SubscriptionTests_WhenSubscriptionTagsAreRemoved_TheyShouldNotBePresent()
    {
        // Arrange 
        var subscriptionId = Guid.NewGuid().ToString();
        await Program.Main(
        [
            "subscription",
            "delete",
            "--id",
            subscriptionId
        ]);

        await Program.Main(
        [
            "subscription",
            "create",
            "--id",
            subscriptionId,
            "--name",
            "sub-test",
            "--tag",
            "test-key=test-value"
        ]);

        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        var armClient = new ArmClient(credentials, subscriptionId, ArmClientOptions);

        // Act
        await topaz.UpdateSubscriptionAsync(Guid.Parse(subscriptionId), "sub-test",
            tags: new Dictionary<string, string>());
        var updatedSubscription = await armClient.GetSubscriptions().GetAsync(subscriptionId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updatedSubscription.Value.Data.Tags, Does.Not.ContainKey("test-key"));
        });
    }

    [Test]
    public async Task SubscriptionTests_WhenSubscriptionIsCancelled_StateShouldBeDisabled()
    {
        // Arrange 
        var subscriptionId = Guid.NewGuid().ToString();
        await Program.Main(
        [
            "subscription",
            "delete",
            "--id",
            subscriptionId
        ]);

        await Program.Main(
        [
            "subscription",
            "create",
            "--id",
            subscriptionId,
            "--name",
            "sub-cancel-test"
        ]);

        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        var armClient = new ArmClient(credentials, subscriptionId, ArmClientOptions);

        // Act
        await topaz.CancelSubscriptionAsync(Guid.Parse(subscriptionId));
        var cancelledSubscription = await armClient.GetSubscriptions().GetAsync(subscriptionId);

        // Assert
        Assert.That(cancelledSubscription.Value.Data.State.ToString(), Is.EqualTo("Disabled"));
    }

    [Test]
    public async Task SubscriptionTests_WhenLocationsAreRequested_TheyAreReturned()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid().ToString();
        await Program.Main(
        [
            "subscription",
            "delete",
            "--id",
            subscriptionId
        ]);

        await Program.Main(
        [
            "subscription",
            "create",
            "--id",
            subscriptionId,
            "--name",
            "sub-locations-test"
        ]);

        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId, ArmClientOptions);

        // Act
        var subscription = await armClient.GetSubscriptions().GetAsync(subscriptionId);
        var locations = new List<Azure.ResourceManager.Resources.Models.SubscriptionLocationData>();
        await foreach (var location in subscription.Value.GetLocationsAsync())
            locations.Add(location);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(locations, Is.Not.Empty);
            Assert.That(locations.Any(l => l.Name == "eastus"), Is.True);
            Assert.That(locations.Any(l => l.Name == "westeurope"), Is.True);
            Assert.That(locations.All(l => l.Id != null), Is.True);
        });
    }
}