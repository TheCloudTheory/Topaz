using Azure.Core;
using Azure.Identity;
using Azure.Local.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace Azure.Local.Tests.E2E;

public class ResourceGroupTests
{
    private static readonly ArmClientOptions armClientOptions = new ArmClientOptions
    {
        Environment = new ArmEnvironment(new Uri("https://localhost:8900"), "https://localhost:8900")
    };

    [Test]
    public void ResourceGroupTests_WhenNewResourceGroupIsCreated_ItShouldReturnBeAvailable()
    {
        // Arrange 
        var credentials = new AzureLocalCredential();
        var armClient = new ArmClient(credentials, Guid.Empty.ToString(), armClientOptions);   
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroups = subscription.GetResourceGroups();

        // Act
        resourceGroups.CreateOrUpdate(WaitUntil.Completed, "rg-test", new ResourceGroupData(AzureLocation.PolandCentral));
    }
}
