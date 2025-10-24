using Azure.ResourceManager;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class SubscriptionTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [Test]
    public async Task SubscriptionTests_WhenSubscriptionIsRequest_ItShouldBeAvailable()
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

        var credentials = new AzureLocalCredential();
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
}
