using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class WhatIfDeploymentTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    /// <summary>
    /// Builds a KV-compatible name from a subscription GUID (must be globally unique across tests).
    /// Format: "kv" + first 14 lowercase hex chars → e.g. "kv90b6f146ef76aa" (16 chars, starts with k).
    /// </summary>
    private static string KvName(Guid subscriptionId) =>
        "kv" + subscriptionId.ToString("N")[..14];

    private static string BuildKvTemplate(string kvName, string skuName = "standard") => $$"""
        {
          "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
          "contentVersion": "1.0.0.0",
          "resources": [
            {
              "type": "Microsoft.KeyVault/vaults",
              "apiVersion": "2024-11-01",
              "name": "{{kvName}}",
              "location": "[resourceGroup().location]",
              "sku": {
                "family": "A",
                "name": "{{skuName}}"
              },
              "properties": {
                "tenantId": "[tenant().tenantId]",
                "accessPolicies": [],
                "enabledForDeployment": false,
                "enabledForTemplateDeployment": false,
                "enabledForDiskEncryption": false,
                "enableRbacAuthorization": true,
                "enableSoftDelete": true,
                "softDeleteRetentionInDays": 90,
                "enablePurgeProtection": false
              }
            }
          ]
        }
        """;

    private const string EmptyTemplate = """
        {
          "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
          "contentVersion": "1.0.0.0",
          "resources": []
        }
        """;

    [Test]
    public async Task WhatIfDeployment_NewResource_ReturnsCreate()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var kvName = KvName(subscriptionId);
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "whatif-sub");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-whatif-create",
            new ResourceGroupData(AzureLocation.WestEurope));

        var deploymentId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/rg-whatif-create/providers/Microsoft.Resources/deployments/whatif-create");
        var deploymentResource = armClient.GetArmDeploymentResource(deploymentId);

        // Act — template creates a new KeyVault that does not yet exist
        var result = await deploymentResource.WhatIfAsync(WaitUntil.Completed,
            new ArmDeploymentWhatIfContent(new ArmDeploymentWhatIfProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(BuildKvTemplate(kvName))
            }));

        // Assert
        Assert.That(result.Value.Changes, Is.Not.Empty, "Expected at least one change.");
        var kvChange = result.Value.Changes.FirstOrDefault(c =>
            c.ResourceId.EndsWith($"Microsoft.KeyVault/vaults/{kvName}", StringComparison.OrdinalIgnoreCase));
        Assert.That(kvChange, Is.Not.Null, "Expected a change for the KeyVault resource.");
        Assert.That(kvChange!.ChangeType.ToString(), Is.EqualTo("Create"));
    }

    [Test]
    public async Task WhatIfDeployment_ExistingResourceUnchanged_ReturnsNoChange()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var kvName = KvName(subscriptionId);
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "whatif-nochange-sub");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-whatif-nochange",
            new ResourceGroupData(AzureLocation.WestEurope));

        // Deploy the KeyVault first
        var deployCollection = rg.Value.GetArmDeployments();
        await deployCollection.CreateOrUpdateAsync(WaitUntil.Completed, "initial-deployment",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(BuildKvTemplate(kvName))
            }));

        // Wait for the deployment to complete
        await Task.Delay(2000);

        // Act — run WhatIf with the same template
        var deploymentId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/rg-whatif-nochange/providers/Microsoft.Resources/deployments/whatif-nochange");
        var deploymentResource = armClient.GetArmDeploymentResource(deploymentId);
        var result = await deploymentResource.WhatIfAsync(WaitUntil.Completed,
            new ArmDeploymentWhatIfContent(new ArmDeploymentWhatIfProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(BuildKvTemplate(kvName))
            }));

        // Assert
        var kvChange = result.Value.Changes.FirstOrDefault(c =>
            c.ResourceId.EndsWith($"Microsoft.KeyVault/vaults/{kvName}", StringComparison.OrdinalIgnoreCase));
        Assert.That(kvChange, Is.Not.Null, "Expected a WhatIf change entry for the KeyVault resource.");
        Assert.That(kvChange!.ChangeType.ToString(), Is.EqualTo("NoChange"));
    }

    [Test]
    public async Task WhatIfDeployment_ExistingResourceModified_ReturnsModifyWithDelta()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var kvName = KvName(subscriptionId);
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "whatif-modify-sub");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-whatif-modify",
            new ResourceGroupData(AzureLocation.WestEurope));

        // Deploy with standard SKU
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, "initial-deployment",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(BuildKvTemplate(kvName, "standard"))
            }));

        await Task.Delay(2000);

        // Act — WhatIf with premium SKU
        var deploymentId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/rg-whatif-modify/providers/Microsoft.Resources/deployments/whatif-modify");
        var deploymentResource = armClient.GetArmDeploymentResource(deploymentId);
        var result = await deploymentResource.WhatIfAsync(WaitUntil.Completed,
            new ArmDeploymentWhatIfContent(new ArmDeploymentWhatIfProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(BuildKvTemplate(kvName, "premium"))
            }));

        // Assert — the KeyVault should show as Modify with a delta for the SKU name
        var kvChange = result.Value.Changes.FirstOrDefault(c =>
            c.ResourceId.EndsWith($"Microsoft.KeyVault/vaults/{kvName}", StringComparison.OrdinalIgnoreCase));
        Assert.That(kvChange, Is.Not.Null, "Expected a WhatIf change entry for the KeyVault resource.");
        Assert.That(kvChange!.ChangeType.ToString(), Is.EqualTo("Modify"));
        Assert.That(kvChange.Delta, Is.Not.Null.And.Not.Empty, "Expected property delta for SKU change.");
    }

    [Test]
    public async Task WhatIfDeployment_CompleteMode_OrphanedResourceShowsDelete()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var kvName = KvName(subscriptionId);
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "whatif-delete-sub");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-whatif-delete",
            new ResourceGroupData(AzureLocation.WestEurope));

        // Deploy a KeyVault
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, "initial-deployment",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(BuildKvTemplate(kvName))
            }));

        await Task.Delay(2000);

        // Act — WhatIf in Complete mode with an empty template (KeyVault not in template → Delete)
        var deploymentId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/rg-whatif-delete/providers/Microsoft.Resources/deployments/whatif-delete");
        var deploymentResource = armClient.GetArmDeploymentResource(deploymentId);
        var result = await deploymentResource.WhatIfAsync(WaitUntil.Completed,
            new ArmDeploymentWhatIfContent(new ArmDeploymentWhatIfProperties(ArmDeploymentMode.Complete)
            {
                Template = BinaryData.FromString(EmptyTemplate)
            }));

        // Assert
        var kvChange = result.Value.Changes.FirstOrDefault(c =>
            c.ResourceId.EndsWith($"Microsoft.KeyVault/vaults/{kvName}", StringComparison.OrdinalIgnoreCase));
        Assert.That(kvChange, Is.Not.Null, "Expected a Delete change for the KeyVault resource.");
        Assert.That(kvChange!.ChangeType.ToString(), Is.EqualTo("Delete"));
    }
}
