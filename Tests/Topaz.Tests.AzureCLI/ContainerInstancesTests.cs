using Topaz.Shared;

namespace Topaz.Tests.AzureCLI;

public class ContainerInstancesTests : TopazFixture
{
    [Test]
    public async Task ContainerInstances_Create_Show_And_Delete()
    {
        const string groupName = "topaz-ci-01";
        const string resourceGroup = "test-ci-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");

        await RunAzureCliCommand(
            $"az container create -n {groupName} -g {resourceGroup} -l westeurope --image nginx --cpu 1 --memory 1",
            resp =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(groupName));
                    Assert.That(resp["provisioningState"]!.GetValue<string>(), Is.EqualTo("Succeeded"));
                });
            });

        await RunAzureCliCommand($"az container show -n {groupName} -g {resourceGroup}", resp =>
        {
            Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(groupName));
        });

        await RunAzureCliCommand($"az container delete -n {groupName} -g {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerInstances_List_ByResourceGroup()
    {
        const string groupName = "topaz-ci-02";
        const string resourceGroup = "test-ci-list-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand($"az container create -n {groupName} -g {resourceGroup} -l westeurope --image alpine --cpu 1 --memory 1");

        await RunAzureCliCommand($"az container list -g {resourceGroup}", resp =>
        {
            var arr = resp.AsArray();
            Assert.That(arr.Any(r => r!["name"]!.GetValue<string>() == groupName), Is.True);
        });

        await RunAzureCliCommand($"az container delete -n {groupName} -g {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerInstances_List_AllSubscriptions()
    {
        const string groupName = "topaz-ci-03";
        const string resourceGroup = "test-ci-listall-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand($"az container create -n {groupName} -g {resourceGroup} -l westeurope --image alpine --cpu 1 --memory 1");

        await RunAzureCliCommand("az container list", resp =>
        {
            var arr = resp.AsArray();
            Assert.That(arr.Any(r => r!["name"]!.GetValue<string>() == groupName), Is.True);
        });

        await RunAzureCliCommand($"az container delete -n {groupName} -g {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerInstances_Start_Stop_Restart()
    {
        const string groupName = "topaz-ci-04";
        const string resourceGroup = "test-ci-ops-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand($"az container create -n {groupName} -g {resourceGroup} -l westeurope --image nginx --cpu 1 --memory 1");

        await RunAzureCliCommand($"az container stop -n {groupName} -g {resourceGroup}");
        await RunAzureCliCommand($"az container start -n {groupName} -g {resourceGroup}");
        await RunAzureCliCommand($"az container restart -n {groupName} -g {resourceGroup}");

        await RunAzureCliCommand($"az container delete -n {groupName} -g {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerInstances_Logs_ReturnsContent()
    {
        const string groupName = "topaz-ci-05";
        const string resourceGroup = "test-ci-logs-rg";
        const string containerName = "nginx";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand($"az container create -n {groupName} -g {resourceGroup} -l westeurope --image nginx --cpu 1 --memory 1");

        await RunAzureCliCommand($"az container logs -n {groupName} -g {resourceGroup} --container-name {containerName}");

        await RunAzureCliCommand($"az container delete -n {groupName} -g {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }
}
