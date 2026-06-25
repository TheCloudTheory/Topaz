using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using System.Text;
using System.Text.Json;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ArmDeploymentReferenceOutputTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [Test]
    public async Task ArmDeployment_WithReferenceOutputOnKeyVault_ReturnsResolvedVaultUri()
    {
        // Arrange
        var kvName = $"kv-ref-out-{Guid.NewGuid():N}"[..24];
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "arm-ref-outputs");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-ref-outputs",
            new ResourceGroupData(AzureLocation.WestEurope));

        // Act
        var template = await File.ReadAllTextAsync("templates/deployment-keyvault-reference-output.json");
        var parameters = BinaryData.FromObjectAsJson(new { keyVaultName = new { value = kvName } });
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, "deploy-ref-output",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(template),
                Parameters = parameters
            }));

        var deployment = await rg.Value.GetArmDeploymentAsync("deploy-ref-output");

        // Assert
        Assert.That(deployment.Value.Data.Properties, Is.Not.Null);
        Assert.That(deployment.Value.Data.Properties.ProvisioningState, Is.EqualTo(ResourcesProvisioningState.Succeeded));
        Assert.That(deployment.Value.Data.Properties.Outputs, Is.Not.Null, "Expected deployment outputs to be populated.");

        var outputsJson = Encoding.UTF8.GetString(deployment.Value.Data.Properties.Outputs!.ToMemory().ToArray());
        var outputs = JsonDocument.Parse(outputsJson).RootElement;

        Assert.That(outputs.TryGetProperty("keyVaultUri", out var kvUriOutput), Is.True, "Expected 'keyVaultUri' in outputs.");
        var kvUriValue = kvUriOutput.GetProperty("value").GetString();
        Assert.That(kvUriValue, Is.Not.Null.And.Contains(kvName), $"Expected vaultUri to contain vault name '{kvName}'.");
        Assert.That(kvUriValue, Does.StartWith("https://"), "Expected vaultUri to be an https URL.");

        Assert.That(outputs.TryGetProperty("keyVaultId", out var kvIdOutput), Is.True, "Expected 'keyVaultId' in outputs.");
        var kvIdValue = kvIdOutput.GetProperty("value").GetString();
        Assert.That(kvIdValue, Is.Not.Null.And.Contains(kvName), "Expected keyVaultId to contain vault name.");
    }

    [Test]
    public async Task ArmDeployment_WithReferenceOutputOnUnknownType_OutputIsNullAndDeploymentSucceeds()
    {
        // reference() on an unknown/unsupported resource type must not fail the deployment.
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "arm-ref-unknown");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-ref-unknown",
            new ResourceGroupData(AzureLocation.WestEurope));

        // Act
        var template = await File.ReadAllTextAsync("templates/deployment-unknown-reference-output.json");
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, "deploy-ref-unknown",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(template)
            }));

        var deployment = await rg.Value.GetArmDeploymentAsync("deploy-ref-unknown");

        // Assert — deployment completes successfully; unresolvable reference outputs null
        Assert.That(deployment.Value.Data.Properties.ProvisioningState, Is.EqualTo(ResourcesProvisioningState.Succeeded));

        var outputsJson = Encoding.UTF8.GetString(deployment.Value.Data.Properties.Outputs!.ToMemory().ToArray());
        var outputs = JsonDocument.Parse(outputsJson).RootElement;
        Assert.That(outputs.TryGetProperty("unknownRef", out var unknownOutput), Is.True);
        Assert.That(unknownOutput.GetProperty("value").ValueKind, Is.EqualTo(JsonValueKind.Null),
            "Expected unresolvable reference() output to be null.");
    }
}

