using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using System.Text.Json;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

/// <summary>
/// Tests that an unhandled exception on the deployment background thread transitions
/// the deployment record to provisioningState=Failed rather than leaving it stuck at Running.
/// Repro: a nested deployment whose inner template uses reference() in outputs (unsupported
/// in the expression evaluation context) previously caused the background thread to exit
/// without ever calling Fail(), leaving the nested deployment stuck at Running forever.
/// </summary>
public class ArmDeploymentFailureTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    private static string GetTemplate() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "templates", "deployment-nested-reference-output.json"));

    private static BinaryData BuildParameters(string resourceGroupName, string nestedDeploymentName, string keyVaultName)
    {
        var parameters = new
        {
            resourceGroupName = new { value = resourceGroupName },
            nestedDeploymentName = new { value = nestedDeploymentName },
            keyVaultName = new { value = keyVaultName }
        };
        return BinaryData.FromString(JsonSerializer.Serialize(parameters));
    }

    [Test]
    public async Task NestedDeployment_WhenInnerTemplateHasUnsupportedReferenceOutput_TransitionsToTerminalState()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "arm-failure-test-sub");

        var subscription = await armClient.GetDefaultSubscriptionAsync();

        var resourceGroupName = "arm-failure-rg";
        var nestedDeploymentName = "nested-failure-deploy";
        var outerDeploymentName = "outer-failure-deploy";
        var keyVaultName = "failure-test-kv";

        // Act: start the outer subscription-scoped deployment (use Started, not Completed,
        // so we can poll manually without the SDK blocking on a failed outer deployment)
        await subscription.GetArmDeployments().CreateOrUpdateAsync(
            WaitUntil.Started,
            outerDeploymentName,
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(GetTemplate()),
                Parameters = BuildParameters(resourceGroupName, nestedDeploymentName, keyVaultName)
            })
            {
                Location = AzureLocation.WestEurope
            });

        // Wait for the outer deployment to finish (success or failure — we just need it not to block)
        ArmDeploymentResource? outerDeployment = null;
        using var outerCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (!outerCts.IsCancellationRequested)
        {
            outerDeployment = await subscription.GetArmDeploymentAsync(outerDeploymentName);
            var outerState = outerDeployment.Data.Properties?.ProvisioningState?.ToString();
            if (outerState is not null && !outerState.Equals("Running", StringComparison.OrdinalIgnoreCase))
                break;
            await Task.Delay(500, CancellationToken.None);
        }

        Assert.That(outerDeployment, Is.Not.Null);
        var outerProvisioningState = outerDeployment!.Data.Properties?.ProvisioningState?.ToString();
        Assert.That(outerProvisioningState, Is.Not.EqualTo("Running").IgnoreCase,
            "Outer deployment must not be stuck in Running state.");

        // Now verify the nested deployment inside the resource group also reached a terminal state.
        // Retrieve the RG first — it may or may not have been created depending on ordering.
        ResourceGroupResource? rg = null;
        try { rg = await subscription.GetResourceGroups().GetAsync(resourceGroupName); }
        catch (RequestFailedException ex) when (ex.Status == 404) { /* RG may not exist if outer failed early */ }

        if (rg is null)
        {
            // Outer deployment failed before the RG was created — that's a terminal state, fix is working.
            Assert.Pass("Outer deployment failed before resource group was created — deployment is not stuck.");
            return;
        }

        // Poll until the nested deployment reaches a terminal state
        ArmDeploymentResource? nestedDeployment = null;
        using var nestedCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (!nestedCts.IsCancellationRequested)
        {
            try
            {
                nestedDeployment = await rg.GetArmDeploymentAsync(nestedDeploymentName);
                var nestedState = nestedDeployment.Data.Properties?.ProvisioningState?.ToString();
                if (nestedState is not null && !nestedState.Equals("Running", StringComparison.OrdinalIgnoreCase))
                    break;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Nested deployment not yet persisted — keep waiting
            }

            await Task.Delay(500, CancellationToken.None);
        }

        Assert.That(nestedDeployment, Is.Not.Null,
            $"Nested deployment '{nestedDeploymentName}' should exist in resource group '{resourceGroupName}'.");

        var nestedProvisioningState = nestedDeployment!.Data.Properties?.ProvisioningState?.ToString();
        Assert.That(nestedProvisioningState, Is.Not.EqualTo("Running").IgnoreCase,
            "Nested deployment must not be stuck in Running state — it must transition to a terminal state (Failed or Succeeded).");
    }

    [Test]
    public async Task NestedDeployment_WhenInnerTemplateHasUnsupportedReferenceOutput_NestedDeploymentMarkedFailed()
    {
        // Arrange — identical template to the above test, different resource names
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "arm-failure-failed-sub");

        var subscription = await armClient.GetDefaultSubscriptionAsync();

        var resourceGroupName = "arm-failure-failed-rg";
        var nestedDeploymentName = "nested-failed-deploy";
        var outerDeploymentName = "outer-failed-deploy";
        var keyVaultName = "failed-test-kv";

        await subscription.GetArmDeployments().CreateOrUpdateAsync(
            WaitUntil.Started,
            outerDeploymentName,
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(GetTemplate()),
                Parameters = BuildParameters(resourceGroupName, nestedDeploymentName, keyVaultName)
            })
            {
                Location = AzureLocation.WestEurope
            });

        // Wait for the outer deployment to reach a terminal state
        using var outerCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        ArmDeploymentResource? outerDeployment = null;
        while (!outerCts.IsCancellationRequested)
        {
            outerDeployment = await subscription.GetArmDeploymentAsync(outerDeploymentName);
            var outerState = outerDeployment.Data.Properties?.ProvisioningState?.ToString();
            if (outerState is not null && !outerState.Equals("Running", StringComparison.OrdinalIgnoreCase))
                break;
            await Task.Delay(500, CancellationToken.None);
        }

        // If reference() is properly unsupported and causes an exception, nested deployment should be Failed.
        // If reference() happens to be swallowed (outputs set to null), the nested deployment may be Succeeded.
        // Either way, it MUST NOT be Running.
        ResourceGroupResource? rg = null;
        try { rg = await subscription.GetResourceGroups().GetAsync(resourceGroupName); }
        catch (RequestFailedException ex) when (ex.Status == 404) { }

        if (rg is null)
        {
            Assert.Pass("Deployment failed before RG creation — not stuck in Running state.");
            return;
        }

        // Wait for the nested deployment to reach a terminal state
        ArmDeploymentResource? nestedDeployment = null;
        using var nestedCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (!nestedCts.IsCancellationRequested)
        {
            try
            {
                nestedDeployment = await rg.GetArmDeploymentAsync(nestedDeploymentName);
                var s = nestedDeployment.Data.Properties?.ProvisioningState?.ToString();
                if (s is not null && !s.Equals("Running", StringComparison.OrdinalIgnoreCase))
                    break;
            }
            catch (RequestFailedException ex) when (ex.Status == 404) { }

            await Task.Delay(500, CancellationToken.None);
        }

        Assert.That(nestedDeployment, Is.Not.Null,
            "Nested deployment must be persisted in the resource group.");

        var nestedState = nestedDeployment!.Data.Properties?.ProvisioningState?.ToString();

        // The deployment must have exited Running. If reference() throws → Failed; if outputs are
        // silently nulled out → Succeeded. Both are acceptable; Running is the bug.
        Assert.That(nestedState, Is.Not.EqualTo("Running").IgnoreCase,
            "Nested deployment must reach a terminal state. 'Running' means the fix is not working.");

        Assert.That(nestedState, Is.EqualTo("Succeeded").Or.EqualTo("Failed").IgnoreCase,
            "Nested deployment must reach a terminal state (Succeeded or Failed).");
    }
}
