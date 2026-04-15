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

    [Test]
    public async Task TableEntity_Delete_RemovesEntityFromTable()
    {
        const string storageAccountName = "topazstortblentdel01";
        const string resourceGroup = "test-storage-table-entity-del-rg";
        const string tableName = "testentities";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
                Assert.That(accountKey, Is.Not.Null.And.Not.Empty);
            });

        await RunAzureCliCommand(
            $"az storage table create --name {tableName} --account-name {storageAccountName} --account-key \"{accountKey}\" --table-endpoint http://{storageAccountName}.table.storage.topaz.local.dev:8890");

        await RunAzureCliCommand(
            $"az storage entity insert --table-name {tableName} --entity PartitionKey=pk1 RowKey=rk1 Name=test --account-name {storageAccountName} --account-key \"{accountKey}\" --table-endpoint http://{storageAccountName}.table.storage.topaz.local.dev:8890");

        await RunAzureCliCommand(
            $"az storage entity delete --table-name {tableName} --partition-key pk1 --row-key rk1 --if-match \"*\" --account-name {storageAccountName} --account-key \"{accountKey}\" --table-endpoint http://{storageAccountName}.table.storage.topaz.local.dev:8890");

        await RunAzureCliCommand(
            $"az storage entity query --table-name {tableName} --account-name {storageAccountName} --account-key \"{accountKey}\" --table-endpoint http://{storageAccountName}.table.storage.topaz.local.dev:8890",
            (resp) =>
            {
                var items = resp["items"]!.AsArray();
                Assert.That(items, Is.Empty);
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task TableEntity_Show_ReturnsEntityByKey()
    {
        const string storageAccountName = "topazstortblshow01";
        const string resourceGroup = "test-storage-table-entity-show-rg";
        const string tableName = "testentities";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
                Assert.That(accountKey, Is.Not.Null.And.Not.Empty);
            });

        await RunAzureCliCommand(
            $"az storage table create --name {tableName} --account-name {storageAccountName} --account-key \"{accountKey}\" --table-endpoint http://{storageAccountName}.table.storage.topaz.local.dev:8890");

        await RunAzureCliCommand(
            $"az storage entity insert --table-name {tableName} --entity PartitionKey=pk1 RowKey=rk1 Name=test --account-name {storageAccountName} --account-key \"{accountKey}\" --table-endpoint http://{storageAccountName}.table.storage.topaz.local.dev:8890");

        await RunAzureCliCommand(
            $"az storage entity show --table-name {tableName} --partition-key pk1 --row-key rk1 --account-name {storageAccountName} --account-key \"{accountKey}\" --table-endpoint http://{storageAccountName}.table.storage.topaz.local.dev:8890",
            (resp) =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(resp["PartitionKey"]!.GetValue<string>(), Is.EqualTo("pk1"));
                    Assert.That(resp["RowKey"]!.GetValue<string>(), Is.EqualTo("rk1"));
                });
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task TableServiceProperties_Set_UpdatesLoggingSettings()
    {
        const string storageAccountName = "topazstortblprops01";
        const string resourceGroup = "test-storage-table-props-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
                Assert.That(accountKey, Is.Not.Null.And.Not.Empty);
            });

        await RunAzureCliCommand(
            $"az storage logging update --services t --log rwd --retention 5 --account-name {storageAccountName} --account-key \"{accountKey}\" --table-endpoint http://{storageAccountName}.table.storage.topaz.local.dev:8890");

        await RunAzureCliCommand(
            $"az storage logging show --services t --account-name {storageAccountName} --account-key \"{accountKey}\" --table-endpoint http://{storageAccountName}.table.storage.topaz.local.dev:8890",
            (resp) =>
            {
                var table = resp["table"]!;
                Assert.Multiple(() =>
                {
                    Assert.That(table["read"]!.GetValue<bool>(), Is.True);
                    Assert.That(table["write"]!.GetValue<bool>(), Is.True);
                    Assert.That(table["delete"]!.GetValue<bool>(), Is.True);
                    Assert.That(table["retentionPolicy"]!["days"]!.GetValue<int>(), Is.EqualTo(5));
                });
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }
}
