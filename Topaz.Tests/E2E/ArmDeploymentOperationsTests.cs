using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ArmDeploymentOperationsTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    private static string BuildManagedIdentityTemplate(string identityName) => $$"""
        {
          "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
          "contentVersion": "1.0.0.0",
          "resources": [
            {
              "type": "Microsoft.ManagedIdentity/userAssignedIdentities",
              "apiVersion": "2018-11-30",
              "name": "{{identityName}}",
              "location": "westeurope"
            }
          ]
        }
        """;

    [Test]
    public async Task DeploymentOperations_RgScope_ListReturnsOperationForEachDeployedResource()
    {
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "ops-rg-list-sub");

        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(
            WaitUntil.Completed, "rg-ops-list", new ResourceGroupData(AzureLocation.WestEurope));

        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(
            WaitUntil.Completed, "deploy-ops-list",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(BuildManagedIdentityTemplate("mi-ops-list"))
            }));

        var deployment = (await rg.Value.GetArmDeploymentAsync("deploy-ops-list")).Value;
        var operations = deployment.GetDeploymentOperations().ToList();

        Assert.That(operations, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(operations[0].OperationId, Is.Not.Null.And.Not.Empty);
            Assert.That(operations[0].Properties?.ProvisioningState, Is.EqualTo("Succeeded"));
            Assert.That(operations[0].Properties?.TargetResource?.ResourceType.ToString(), Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task DeploymentOperations_RgScope_EmptyTemplateReturnsNoOperations()
    {
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "ops-rg-empty-sub");

        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(
            WaitUntil.Completed, "rg-ops-empty", new ResourceGroupData(AzureLocation.WestEurope));

        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(
            WaitUntil.Completed, "deploy-ops-empty",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(
                    """{"$schema":"https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#","contentVersion":"1.0.0.0","resources":[]}""")
            }));

        var deployment = (await rg.Value.GetArmDeploymentAsync("deploy-ops-empty")).Value;
        var operations = deployment.GetDeploymentOperations().ToList();

        Assert.That(operations, Is.Empty);
    }

    [Test]
    public async Task DeploymentOperations_SubscriptionScope_ListReturnsOperationsForDeployedResources()
    {
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "ops-sub-list-sub");

        var subscription = await armClient.GetDefaultSubscriptionAsync();

        const string template = """
            {
              "$schema": "https://schema.management.azure.com/schemas/2018-05-01/subscriptionDeploymentTemplate.json#",
              "contentVersion": "1.0.0.0",
              "resources": [
                {
                  "type": "Microsoft.Resources/resourceGroups",
                  "apiVersion": "2021-04-01",
                  "name": "rg-ops-sub-created",
                  "location": "westeurope"
                }
              ]
            }
            """;

        await subscription.GetArmDeployments().CreateOrUpdateAsync(
            WaitUntil.Completed, "deploy-sub-ops-list",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(template)
            })
            { Location = AzureLocation.WestEurope });

        var deployment = (await subscription.GetArmDeploymentAsync("deploy-sub-ops-list")).Value;
        var operations = deployment.GetDeploymentOperations().ToList();

        Assert.That(operations, Is.Not.Empty);
        Assert.That(operations[0].OperationId, Is.Not.Null.And.Not.Empty);
    }
}
