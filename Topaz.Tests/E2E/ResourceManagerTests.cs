using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
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

    [Test]
    public async Task ResourceManagerTest_WhenDeploymentIsRequested_ItShouldBeAvailable()
    {
        // Arrange
        const string subscriptionName = "test-sub";
        const string resourceGroupName = "rg-deployment";
        const string deploymentName = "deployment";
        
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential();
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient();
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName,
            new ResourceGroupData(AzureLocation.WestEurope));

        // Act
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, deploymentName,
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(await File.ReadAllTextAsync("templates/deployment1.json"))
            }));
        var deployment = await rg.Value.GetArmDeploymentAsync(deploymentName);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deployment.Value.Data.Name, Is.EqualTo(deploymentName));
        });
    }

    [Test]
    public async Task ResourceManagerTest_WhenDeploymentIsDeleted_ItShouldNotBeAvailable()
    {
        // Arrange
        const string subscriptionName = "test-sub";
        const string resourceGroupName = "rg-deployment";
        const string deploymentName = "deployment-to-delete";
        
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential();
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient();
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName,
            new ResourceGroupData(AzureLocation.WestEurope));
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, deploymentName,
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(await File.ReadAllTextAsync("templates/deployment1.json"))
            }));

        // Act
        await (await rg.Value.GetArmDeploymentAsync(deploymentName)).Value.DeleteAsync(WaitUntil.Completed);
        var deployments = rg.Value.GetArmDeployments().Where(deployment => deployment.Data.Name.Equals(deploymentName));
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deployments.Count, Is.EqualTo(0));
        });
    }
}