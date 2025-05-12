using Azure.Core;
using Topaz.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure;

namespace Topaz.Tests.E2E;

public class ResourceGroupTests
{
    private static readonly ArmClientOptions armClientOptions = new ArmClientOptions
    {
        Environment = new ArmEnvironment(new Uri("https://localhost:8900"), "https://localhost:8900")
    };

    [SetUp]
    public async Task SetUp()
    {
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

        await Program.Main([
                "group",
                "delete",
                "--name",
                "rg-test"
            ]);
    }

    [Test]
    public void ResourceGroupTests_WhenNewResourceGroupIsCreated_ItShouldReturnBeAvailable()
    {
        // Arrange 
        var credentials = new AzureLocalCredential();
        var armClient = new ArmClient(credentials, Guid.Empty.ToString(), armClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroups = subscription.GetResourceGroups();

        // Act
        var operation = resourceGroups.CreateOrUpdate(WaitUntil.Completed, "rg-test", new ResourceGroupData(AzureLocation.PolandCentral));
        var rg = resourceGroups.Get("rg-test");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(rg.Value.Data.Name, Is.EqualTo("rg-test"));
            Assert.That(operation.Value.Data.Name, Is.EqualTo("rg-test"));
        });
    }

    [Test]
    public void ResourceGroupTests_WhenResourceGroupIsCheckedForExistence_ItShouldReturnCorrectResult()
    {
        // Arrange 
        var credentials = new AzureLocalCredential();
        var armClient = new ArmClient(credentials, Guid.Empty.ToString(), armClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroups = subscription.GetResourceGroups();
        
        resourceGroups.CreateOrUpdate(WaitUntil.Completed, "rg-test", new ResourceGroupData(AzureLocation.PolandCentral));

        // Act
        var exist = resourceGroups.Exists("rg-test");

        // Assert
        Assert.That(exist);
    }
}
