using Azure.ResourceManager;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ResourceManagerTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    
    [Test]
    public async Task ResourceManagerTest_WhenSubscriptionIsCreatedUsingArmClient_ItShouldBeAvailable()
    {
        // Arrange
        const string subscriptionName = "test-sub";
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential();
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient();
        
        // Act
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(subscription.Data.Id.ToString(), Is.EqualTo($"/subscriptions/{subscriptionId}"));
            Assert.That(subscription.Data.SubscriptionId, Is.EqualTo(subscriptionId.ToString()));
            Assert.That(subscription.Data.DisplayName, Is.EqualTo(subscriptionName));
        });
    }
}