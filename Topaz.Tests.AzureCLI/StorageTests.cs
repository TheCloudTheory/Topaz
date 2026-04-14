namespace Topaz.Tests.AzureCLI;

public class StorageTests : TopazFixture
{
    [Test]
    public async Task StorageAccount_CheckName_Available()
    {
        await RunAzureCliCommand("az storage account check-name --name topazstoragechk01", (resp) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(resp["nameAvailable"]!.GetValue<bool>(), Is.True);
                Assert.That(resp["reason"], Is.Null);
            });
        });
    }

    [Test]
    public async Task StorageAccount_CheckName_AlreadyExists()
    {
        const string storageAccountName = "topazstoragechk02";
        const string resourceGroup = "test-storage-check-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        await RunAzureCliCommand($"az storage account check-name --name {storageAccountName}", (resp) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(resp["nameAvailable"]!.GetValue<bool>(), Is.False);
                Assert.That(resp["reason"]!.GetValue<string>(), Is.EqualTo("AlreadyExists"));
            });
        });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

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

    [Test]
    public async Task StorageAccount_Update_AppliesTags()
    {
        const string storageAccountName = "topazstorageupd01";
        const string resourceGroup = "test-storage-update-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        await RunAzureCliCommand(
            $"az storage account update --name {storageAccountName} --resource-group {resourceGroup} --tags env=test",
            (resp) =>
            {
                Assert.That(resp["tags"]!["env"]!.GetValue<string>(), Is.EqualTo("test"));
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task StorageAccount_RegenerateKey_ReturnsNewKeyValue()
    {
        const string storageAccountName = "topazstorregenkey01";
        const string resourceGroup = "test-storage-regen-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? originalKey1 = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                originalKey1 = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
                Assert.That(originalKey1, Is.Not.Null.And.Not.Empty);
            });

        await RunAzureCliCommand(
            $"az storage account keys renew --account-name {storageAccountName} --resource-group {resourceGroup} --key primary",
            (resp) =>
            {
                var newKey1 = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
                Assert.That(newKey1, Is.Not.EqualTo(originalKey1));
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task StorageAccount_GenerateAccountSas_ReturnsTokenWithExpectedParameters()
    {
        const string storageAccountName = "topazstorsas01";
        const string resourceGroup = "test-storage-sas-rg";
        var expiry = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ssZ");

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        await RunAzureCliCommand(
            $"az storage account generate-sas --account-name {storageAccountName} --services b --resource-types s --permissions r --expiry {expiry} --https-only",
            (resp) =>
            {
                var token = resp.GetValue<string>();
                Assert.That(token, Is.Not.Null.And.Not.Empty);
                Assert.That(token, Does.Contain("sv="));
                Assert.That(token, Does.Contain("sig="));
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }
}
