using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ResourceGroupIdentifierTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";

    [SetUp]
    public async Task SetUp()
    {
        await Program.Main(
        [
            "subscription",
            "delete",
            "--id",
            SubscriptionId.ToString()
        ]);

        await Program.Main(
        [
            "subscription",
            "create",
            "--id",
            SubscriptionId.ToString(),
            "--name",
            SubscriptionName
        ]);

        await Program.Main([
                "group",
                "delete",
                "--name",
                ResourceGroupName
            ]);
    }

    [Test]
    public void ResourceGroupTests_WhenNewResourceGroupIsCreated_ItShouldReturnBeAvailable()
    {
        // Arrange 
        var credentials = new AzureLocalCredential();
        var armClient = new ArmClient(credentials, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroups = subscription.GetResourceGroups();

        // Act
        var operation = resourceGroups.CreateOrUpdate(WaitUntil.Completed, ResourceGroupName, new ResourceGroupData(AzureLocation.PolandCentral));
        var rg = resourceGroups.Get(ResourceGroupName);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(rg.Value.Data.Name, Is.EqualTo(ResourceGroupName));
            Assert.That(operation.Value.Data.Name, Is.EqualTo(ResourceGroupName));
        });
    }

    [Test]
    public void ResourceGroupTests_WhenResourceGroupIsCheckedForExistence_ItShouldReturnCorrectResult()
    {
        // Arrange 
        var credentials = new AzureLocalCredential();
        var armClient = new ArmClient(credentials, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroups = subscription.GetResourceGroups();
        
        resourceGroups.CreateOrUpdate(WaitUntil.Completed, ResourceGroupName, new ResourceGroupData(AzureLocation.PolandCentral));

        // Act
        var exist = resourceGroups.Exists(ResourceGroupName);

        // Assert
        Assert.That(exist);
    }
    
    [Test]
    public void ResourceGroupTests_WhenMultipleResourceGroupsAreCreated_TheyShouldBeReturnedWhenListingThem()
    {
        // Arrange
        var names = new [] { "rg1", "rg2", "rg3" };
        var credentials = new AzureLocalCredential();
        var armClient = new ArmClient(credentials, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroups = subscription.GetResourceGroups();

        foreach (var name in names)
        {
            if (resourceGroups.Exists(name).Value)
            {
                resourceGroups.Get(name).Value.Delete(WaitUntil.Completed);
            }
            
            resourceGroups.CreateOrUpdate(WaitUntil.Completed, name, new ResourceGroupData(AzureLocation.WestEurope));
        }

        // Act
        var list = subscription.GetResourceGroups().GetAll().ToArray();

        // Assert
        Assert.That(list, Has.Length.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(list.Any(g => g.Data.Name == names[0]));
            Assert.That(list.Any(g => g.Data.Name == names[1]));
            Assert.That(list.Any(g => g.Data.Name == names[2]));
        });
    }
}
