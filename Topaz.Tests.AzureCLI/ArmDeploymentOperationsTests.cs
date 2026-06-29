namespace Topaz.Tests.AzureCLI;

public class ArmDeploymentOperationsTests : TopazFixture
{
    // --- resource-group scope ---

    [Test]
    public async Task DeploymentOperations_RgScope_ListReturnsOperationsAfterDeployment()
    {
        await RunAzureCliCommand("az group create -n rg-ops-cli-list -l westeurope");
        await RunAzureCliCommand(
            "az deployment group create --name deploy-ops-cli-list -g rg-ops-cli-list --template-file \"/templates/deployment-with-identity.json\"");

        await RunAzureCliCommand(
            "az deployment operation group list --name deploy-ops-cli-list -g rg-ops-cli-list",
            response =>
            {
                Assert.That(response.AsArray(), Is.Not.Empty,
                    "Expected at least one operation record after deploying a resource.");
                Assert.That(response[0]?["operationId"]?.GetValue<string>(), Is.Not.Null.And.Not.Empty);
            });

        await RunAzureCliCommand("az group delete -n rg-ops-cli-list --yes");
    }

    [Test]
    public async Task DeploymentOperations_RgScope_ShowByIdReturnsMatchingOperation()
    {
        await RunAzureCliCommand("az group create -n rg-ops-cli-show -l westeurope");
        await RunAzureCliCommand(
            "az deployment group create --name deploy-ops-cli-show -g rg-ops-cli-show --template-file \"/templates/deployment-with-identity.json\"");

        string operationId = string.Empty;
        await RunAzureCliCommand(
            "az deployment operation group list --name deploy-ops-cli-show -g rg-ops-cli-show",
            response =>
            {
                operationId = response[0]!["operationId"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az deployment operation group show --name deploy-ops-cli-show -g rg-ops-cli-show --operation-ids {operationId}",
            response =>
            {
                // az deployment operation group show returns an array of matched operations
                Assert.That(response.AsArray(), Is.Not.Empty);
                Assert.That(response[0]?["operationId"]?.GetValue<string>(), Is.EqualTo(operationId));
            });

        await RunAzureCliCommand("az group delete -n rg-ops-cli-show --yes");
    }

    // --- subscription scope ---

    [Test]
    public async Task DeploymentOperations_SubscriptionScope_ListReturnsOperations()
    {
        await RunAzureCliCommand(
            "az deployment sub create --name deploy-ops-sub-list --location westeurope --template-file \"/templates/deployment-with-identity.json\"");

        await RunAzureCliCommand(
            "az deployment operation sub list --name deploy-ops-sub-list",
            response =>
            {
                Assert.That(response.AsArray(), Is.Not.Empty,
                    "Expected at least one operation record for subscription-scope deployment.");
            });
    }

    // --- management group scope ---

    [Test]
    public async Task DeploymentOperations_ManagementGroupScope_ListReturnsEmptyForEmptyTemplate()
    {
        await RunAzureCliCommand(
            "az account management-group create --name mg-ops-cli-list");
        await RunAzureCliCommand(
            "az deployment mg create --name deploy-ops-mg-list --management-group-id mg-ops-cli-list --location westeurope --template-file \"/templates/empty-deployment.json\"");

        await RunAzureCliCommand(
            "az deployment operation mg list --name deploy-ops-mg-list --management-group-id mg-ops-cli-list",
            response =>
            {
                Assert.That(response.AsArray(), Is.Not.Null,
                    "Expected a valid (possibly empty) operations array for management-group-scope deployment.");
            });
    }

    [Test]
    public async Task DeploymentOperations_ManagementGroupScope_UnknownDeploymentReturns404()
    {
        await RunAzureCliCommand(
            "az account management-group create --name mg-ops-cli-404");

        await RunAzureCliCommand(
            "az rest --method GET --url 'https://topaz.local.dev:8899/providers/Microsoft.Management/managementGroups/mg-ops-cli-404/providers/Microsoft.Resources/deployments/nonexistent-deploy/operations?api-version=2021-04-01' --skip-authorization-header",
            exitCode: 1);
    }
}
