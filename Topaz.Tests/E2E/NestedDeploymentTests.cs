using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class NestedDeploymentTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    private static string GetNestedDeploymentTemplate() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "templates", "deployment-nested.json"));

    [Test]
    public async Task NestedDeployment_SubscriptionScopeWithInlineTemplate_InnerResourcesProvisioned()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "nested-deploy-sub");
        
        // Act: Deploy the subscription-scoped template
        var subscriptionResource = armClient.GetSubscriptionResource(ResourceIdentifier.Root);
        await subscriptionResource.GetArmDeployments().CreateOrUpdateAsync(
            WaitUntil.Completed,
            "sub-deploy-with-nested",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(GetNestedDeploymentTemplate())
            }));

        var subscription = await armClient.GetDefaultSubscriptionAsync();

        // Assert: Verify the resource group was created
        var targetRgName = "nested-kv-rg";
        ResourceGroupResource? targetRg = null;
        try
        {
            targetRg = await subscription.GetResourceGroups().GetAsync(targetRgName);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            Assert.Fail($"Target resource group '{targetRgName}' was not created.");
        }

        // Assert: Verify the Key Vault was created inside the target resource group
        KeyVaultResource? keyVault = null;
        try
        {
            var kvCollection = targetRg.GetKeyVaults();
            await foreach (var kv in kvCollection.GetAllAsync())
            {
                if (kv.Data.Name.Contains("nestedkvtest"))
                {
                    keyVault = kv;
                    break;
                }
            }
        }
        catch
        {
            // Expected if no KeyVaults exist
        }

        Assert.That(keyVault, Is.Not.Null, "Expected Key Vault 'nestedkvtest' to exist in the target resource group.");

        // Assert: Verify the nested deployment resource exists and has succeeded
        var nestedDeploymentName = "nested-kv-deploy";
        ArmDeploymentResource? nestedDeployment = null;
        try
        {
            nestedDeployment = await targetRg.GetArmDeploymentAsync(nestedDeploymentName);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            Assert.Fail($"Nested deployment resource '{nestedDeploymentName}' was not created.");
        }

        Assert.That(nestedDeployment, Is.Not.Null);
        Assert.That(nestedDeployment.Data.Properties.ProvisioningState?.ToString(), Is.EqualTo("Succeeded").IgnoreCase,
            "Expected nested deployment to have succeeded.");
    }
}
