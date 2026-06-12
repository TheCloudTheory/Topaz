using Topaz.Shared;

namespace Topaz.Tests.AzureCLI;

public class NestedDeploymentTests : TopazFixture
{
    private static string GetNestedDeploymentTemplate() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "templates", "deployment-nested.json"));

    [Test]
    public async Task NestedDeployment_SubscriptionScopeWithInlineTemplate_InnerResourcesProvisioned()
    {
        // Create a temporary file with the template
        var templateFile = Path.Combine(Path.GetTempPath(), $"nested-deploy-{Guid.NewGuid()}.json");
        await File.WriteAllTextAsync(templateFile, GetNestedDeploymentTemplate());

        try
        {
            // Deploy at subscription scope
            await RunAzureCliCommand(
                $"az deployment sub create --name nested-deploy-test --location westeurope --template-file {templateFile}",
                response =>
                {
                    Assert.That(response["properties"]?["provisioningState"]?.GetValue<string>(),
                        Is.EqualTo("Succeeded").IgnoreCase, "Expected subscription deployment to succeed.");
                });

            // Verify the resource group was created
            await RunAzureCliCommand(
                "az group show --name nested-kv-rg",
                response =>
                {
                    Assert.That(response["name"]?.GetValue<string>(),
                        Is.EqualTo("nested-kv-rg").IgnoreCase);
                });

            // Verify the Key Vault was created
            await RunAzureCliCommand(
                "az keyvault show --resource-group nested-kv-rg --name nestedkvtest",
                response =>
                {
                    Assert.That(response["name"]?.GetValue<string>(),
                        Is.EqualTo("nestedkvtest").IgnoreCase);
                });

            // Verify the nested deployment resource exists
            await RunAzureCliCommand(
                "az deployment group show --resource-group nested-kv-rg --name nested-kv-deploy",
                response =>
                {
                    Assert.That(response["properties"]?["provisioningState"]?.GetValue<string>(),
                        Is.EqualTo("Succeeded").IgnoreCase, "Expected nested deployment to have succeeded.");
                });
        }
        finally
        {
            // Clean up template file
            if (File.Exists(templateFile))
            {
                File.Delete(templateFile);
            }
        }
    }
}
