namespace Topaz.Tests.AzureCLI;

public class StorageTests : TopazFixture
{
    [Test]
    public async Task StorageAccount_List_BySubscription_ReturnsCreatedAccount()
    {
        const string storageAccountName = "topazstorlistsub01";
        const string resourceGroup = "test-storage-list-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");

        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS",
            (resp) =>
            {
                Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(storageAccountName));
            });

        await RunAzureCliCommand("az storage account list", (resp) =>
        {
            var arr = resp.AsArray();
            Assert.That(arr.Any(r => r!["name"]!.GetValue<string>() == storageAccountName), Is.True);
        });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }
}
