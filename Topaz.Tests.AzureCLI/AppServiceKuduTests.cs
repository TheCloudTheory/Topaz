namespace Topaz.Tests.AzureCLI;

public class AppServiceKuduTests : TopazFixture
{
    private const string KuduPort = "8896";

    [Test]
    public async Task AppServiceKuduTests_ZipDeploy_ReturnsAccepted()
    {
        var subscriptionId = string.Empty;
        await RunAzureCliCommand("az group create -n rg-kudu-deploy -l westeurope");
        await RunAzureCliCommand("az appservice plan create -n plan-kudu-deploy -g rg-kudu-deploy --sku B1 -l westeurope");
        await RunAzureCliCommand("az webapp create -n kudu-cli-deploy -g rg-kudu-deploy --plan plan-kudu-deploy");
        await RunAzureCliCommand("az account show", response =>
        {
            subscriptionId = response["id"]!.GetValue<string>();
        });
        
        var user = string.Empty;
        var password = string.Empty;
        await RunAzureCliCommand($"az webapp deployment list-publishing-credentials -n kudu-cli-deploy -g rg-kudu-deploy -s {subscriptionId}",
            response =>
            {
                user = response["publishingUserName"]!.GetValue<string>();
                password = response["publishingPassword"]!.GetValue<string>();
            });

        var deployUrl = $"https://kudu-cli-deploy.scm.azurewebsites.topaz.local.dev:{KuduPort}/api/zipdeploy";
        var shellUser = user.Replace("$", "\\$");
        await RunAzureCliCommand(
            $"curl -sk -o /dev/null -w '{{\"status\":%{{http_code}}}}' -X POST \"{deployUrl}\" -u \"{shellUser}:{password}\" --data-binary ''",
            response =>
            {
                Assert.That(response["status"]!.GetValue<int>(), Is.EqualTo(202));
            });

        await RunAzureCliCommand("az group delete -n rg-kudu-deploy --yes");
    }

    [Test]
    public async Task AppServiceKuduTests_GetDeployments_AfterZipDeploy_ReturnsList()
    {
        await RunAzureCliCommand("az group create -n rg-kudu-list -l westeurope");
        await RunAzureCliCommand("az appservice plan create -n plan-kudu-list -g rg-kudu-list --sku B1 -l westeurope");
        await RunAzureCliCommand("az webapp create -n kudu-cli-list -g rg-kudu-list --plan plan-kudu-list");

        var subscriptionId = string.Empty;
        await RunAzureCliCommand("az account show", response =>
        {
            subscriptionId = response["id"]!.GetValue<string>();
        });

        var user = string.Empty;
        var password = string.Empty;
        await RunAzureCliCommand($"az webapp deployment list-publishing-credentials -n kudu-cli-list -g rg-kudu-list -s {subscriptionId}",
            response =>
            {
                user = response["publishingUserName"]!.GetValue<string>();
                password = response["publishingPassword"]!.GetValue<string>();
            });

        var deployUrl = $"https://kudu-cli-list.scm.azurewebsites.topaz.local.dev:{KuduPort}/api/zipdeploy";
        var listUrl = $"https://kudu-cli-list.scm.azurewebsites.topaz.local.dev:{KuduPort}/api/deployments";
        var shellUser = user.Replace("$", "\\$");

        await RunAzureCliCommand($"curl -sk -o /dev/null -w '{{\"status\":%{{http_code}}}}' -X POST \"{deployUrl}\" -u \"{shellUser}:{password}\" --data-binary ''");

        await RunAzureCliCommand(
            $"curl -sk -u \"{shellUser}:{password}\" \"{listUrl}\"",
            response =>
            {
                var array = response.AsArray();
                Assert.That(array, Is.Not.Null);
                Assert.That(array!.Count, Is.GreaterThanOrEqualTo(1));
                Assert.That(array[0]!["status"]!.GetValue<string>(), Is.EqualTo("succeeded"));
                Assert.That(array[0]!["deployer"]!.GetValue<string>(), Is.EqualTo("Push Deployer"));
            });

        await RunAzureCliCommand("az group delete -n rg-kudu-list --yes");
    }
}
