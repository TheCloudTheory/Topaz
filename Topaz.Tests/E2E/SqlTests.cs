using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class SqlTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [Test]
    public async Task SqlServer_Deploy_ShouldSucceedWhenTemplateContainsSqlServer()
    {
        // Arrange
        const string resourceGroupName = "rg-sql-scaffold";
        const string deploymentName = "deployment-sql-server";

        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "sub-sql-scaffold");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(
            WaitUntil.Completed,
            resourceGroupName,
            new ResourceGroupData(AzureLocation.WestEurope));

        // Act
        var deployment = await rg.Value.GetArmDeployments().CreateOrUpdateAsync(
            WaitUntil.Completed,
            deploymentName,
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(await File.ReadAllTextAsync("templates/deployment-sql-server.json"))
            }));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deployment.Value.Data.Name, Is.EqualTo(deploymentName));
            Assert.That(
                deployment.Value.Data.Properties.ProvisioningState.ToString(),
                Is.EqualTo("Succeeded").IgnoreCase);
        });
    }
}
