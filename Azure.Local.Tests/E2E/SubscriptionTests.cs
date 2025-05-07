using Azure.Local.Identity;
using Azure.ResourceManager;

namespace Azure.Local.Tests.E2E;

public class SubscriptionTests
{
    private static readonly ArmClientOptions armClientOptions = new ArmClientOptions
    {
        Environment = new ArmEnvironment(new Uri("https://localhost:8900"), "https://localhost:8900")
    };

    [Test]
    public void SubscriptionTests_WhenSubscriptionIsRequest_ItShouldBeAvailable()
    {
        // Arrange 
        var credentials = new AzureLocalCredential();
        var armClient = new ArmClient(credentials, Guid.Empty.ToString(), armClientOptions);
        
        // Act
        var subscription = armClient.GetDefaultSubscription();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(subscription.Data.Id.ToString(), Is.EqualTo($"/subscriptions/{Guid.Empty}"));
            Assert.That(subscription.Data.SubscriptionId.ToString(), Is.EqualTo(Guid.Empty.ToString()));
        });
    }
}
