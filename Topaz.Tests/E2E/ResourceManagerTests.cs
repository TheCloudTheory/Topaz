using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.ManagedServiceIdentities;
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
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        
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
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
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
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
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

    [Test]
    public async Task ResourceManagerTest_WhenTemplateContainsSupportedResource_ItShouldBeDeployed()
    {
        // Arrange
        const string subscriptionName = "test-sub";
        const string resourceGroupName = "rg-deployment";
        const string deploymentName = "deployment-keyvault";
            
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName,
            new ResourceGroupData(AzureLocation.WestEurope));

        // Act
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, deploymentName,
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(await File.ReadAllTextAsync("templates/deployment-keyvault.json"))
            }));
        
        // Assert
        var kv = await rg.Value.GetKeyVaultAsync("topaz-keyvault");

        Assert.Multiple(() =>
        {
            Assert.That(kv, Is.Not.Null);
            Assert.That(kv.Value.Data.Name, Is.EqualTo("topaz-keyvault"));
        });
        
        // Cleanup
        await kv.Value.DeleteAsync(WaitUntil.Completed);
        await topaz.PurgeKeyVault(subscriptionId, kv.Value.Data.Name, kv.Value.Data.Location);
    }
    
    [Test]
    public async Task ResourceManagerTest_WhenTemplateContainsParameters_TheyShouldBeSupported()
    {
        // Arrange
        const string subscriptionName = "test-sub";
        const string resourceGroupName = "rg-deployment";
        const string deploymentName = "deployment-parameters";
            
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName,
            new ResourceGroupData(AzureLocation.WestEurope));

        // Act
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, deploymentName,
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(await File.ReadAllTextAsync("templates/deployment-identity.json")),
                Parameters = BinaryData.FromString(await File.ReadAllTextAsync("templates/deployment-identity.parameters.json"))
            }));
        
        // Assert
        var kv = await rg.Value.GetKeyVaultAsync("deploykeyvault01");
        var mi = await rg.Value.GetUserAssignedIdentityAsync("deployidentity01");

        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(kv, Is.Not.Null);
                Assert.That(kv.Value.Data.Name, Is.EqualTo("deploykeyvault01"));
                Assert.That(mi, Is.Not.Null);
                Assert.That(mi.Value.Data.Name, Is.EqualTo("deployidentity01"));
                Assert.That(mi.Value.Data.Tags, Contains.Key("cost-center"));
                Assert.That(mi.Value.Data.Tags["cost-center"], Is.EqualTo("10008923"));
            });
        }
        finally
        {
            // Cleanup
            await kv.Value.DeleteAsync(WaitUntil.Completed);
            await topaz.PurgeKeyVault(subscriptionId, kv.Value.Data.Name, kv.Value.Data.Location);
        }
    }
}