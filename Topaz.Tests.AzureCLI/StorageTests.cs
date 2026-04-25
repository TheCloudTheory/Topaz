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
            $"az storage table create --name {tableName} --account-name {storageAccountName} --account-key \"{accountKey}\" --table-endpoint https://{storageAccountName}.table.storage.topaz.local.dev:8890");

        await RunAzureCliCommand(
            $"az storage entity insert --table-name {tableName} --entity PartitionKey=pk1 RowKey=rk1 Name=test --account-name {storageAccountName} --account-key \"{accountKey}\" --table-endpoint https://{storageAccountName}.table.storage.topaz.local.dev:8890");

        await RunAzureCliCommand(
            $"az storage entity delete --table-name {tableName} --partition-key pk1 --row-key rk1 --if-match \"*\" --account-name {storageAccountName} --account-key \"{accountKey}\" --table-endpoint https://{storageAccountName}.table.storage.topaz.local.dev:8890");

        await RunAzureCliCommand(
            $"az storage entity query --table-name {tableName} --account-name {storageAccountName} --account-key \"{accountKey}\" --table-endpoint https://{storageAccountName}.table.storage.topaz.local.dev:8890",
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
            $"az storage table create --name {tableName} --account-name {storageAccountName} --account-key \"{accountKey}\" --table-endpoint https://{storageAccountName}.table.storage.topaz.local.dev:8890");

        await RunAzureCliCommand(
            $"az storage entity insert --table-name {tableName} --entity PartitionKey=pk1 RowKey=rk1 Name=test --account-name {storageAccountName} --account-key \"{accountKey}\" --table-endpoint https://{storageAccountName}.table.storage.topaz.local.dev:8890");

        await RunAzureCliCommand(
            $"az storage entity show --table-name {tableName} --partition-key pk1 --row-key rk1 --account-name {storageAccountName} --account-key \"{accountKey}\" --table-endpoint https://{storageAccountName}.table.storage.topaz.local.dev:8890",
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
            $"az storage logging update --services t --log rwd --retention 5 --connection-string \"AccountName={storageAccountName};AccountKey={accountKey};TableEndpoint=https://{storageAccountName}.table.storage.topaz.local.dev:8890\"");

        await RunAzureCliCommand(
            $"az storage logging show --services t --connection-string \"AccountName={storageAccountName};AccountKey={accountKey};TableEndpoint=https://{storageAccountName}.table.storage.topaz.local.dev:8890\"",
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

    [Test]
    public async Task Blob_Show_ReturnsExpectedProperties()
    {
        const string storageAccountName = "topazstorblobshow01";
        const string resourceGroup = "test-storage-blob-show-rg";
        const string containerName = "blobshowcontainer";

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
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"printf 'hello blob show' >/tmp/blob-show.txt && az storage blob upload --container-name {containerName} --name show.txt --file /tmp/blob-show.txt --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob show --container-name {containerName} --name show.txt --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo("show.txt"));
                    Assert.That(resp["properties"]!["contentLength"]!.GetValue<long>(), Is.GreaterThan(0));
                    Assert.That(resp["properties"]!["contentSettings"]!["contentType"]!.GetValue<string>(), Is.Not.Null.And.Not.Empty);
                });
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task Blob_MetadataShow_ReturnsSetMetadata()
    {
        const string storageAccountName = "topazstorblobmeta01";
        const string resourceGroup = "test-storage-blob-meta-rg";
        const string containerName = "blobmetacontainer";

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
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"printf 'hello metadata' >/tmp/blob-meta.txt && az storage blob upload --container-name {containerName} --name meta.txt --file /tmp/blob-meta.txt --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob metadata update --container-name {containerName} --name meta.txt --metadata env=staging version=2 --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob metadata show --container-name {containerName} --name meta.txt --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(resp["env"]!.GetValue<string>(), Is.EqualTo("staging"));
                    Assert.That(resp["version"]!.GetValue<string>(), Is.EqualTo("2"));
                });
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task Blob_Update_ContentTypePersists()
    {
        const string storageAccountName = "topazstorblobupd01";
        const string resourceGroup = "test-storage-blob-upd-rg";
        const string containerName = "blobupdate";

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
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"printf 'hello update' >/tmp/blob-update.txt && az storage blob upload --container-name {containerName} --name update.txt --file /tmp/blob-update.txt --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        // Use --connection-string with explicit BlobEndpoint so the Azure CLI content-settings
        // validator (get_content_setting_validator in _validators.py) passes the full endpoint URL
        // when pre-fetching current blob properties.  Without it the validator builds account_kwargs
        // without account_url, causing get_account_url() to construct an https:// URL to the wrong
        // port (8890) which produces an SSL record-layer failure.
        await RunAzureCliCommand(
            $"az storage blob update --container-name {containerName} --name update.txt --content-type text/plain --content-encoding utf-8 --connection-string \"AccountName={storageAccountName};AccountKey={accountKey};BlobEndpoint=http://{storageAccountName}.blob.storage.topaz.local.dev:8891\"");

        await RunAzureCliCommand(
            $"az storage blob show --container-name {containerName} --name update.txt --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo("update.txt"));
                    Assert.That(resp["properties"]!["contentSettings"]!["contentType"]!.GetValue<string>(), Is.EqualTo("text/plain"));
                    Assert.That(resp["properties"]!["contentSettings"]!["contentEncoding"]!.GetValue<string>(), Is.EqualTo("utf-8"));
                });
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task Container_SetMetadata_StoresMetadataSuccessfully()
    {
        const string storageAccountName = "topazstorcntrmeta01";
        const string resourceGroup = "test-storage-cntr-meta-rg";
        const string containerName = "metacontainer";

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
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage container metadata update --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --metadata env=prod owner=team-a --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task Blob_List_ReturnsUploadedBlob()
    {
        const string storageAccountName = "topazstorbloblist01";
        const string resourceGroup = "test-storage-blob-list-rg";
        const string containerName = "bloblistcontainer";

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
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"printf 'hello world' >/tmp/blob-list-upload.txt && az storage blob upload --container-name {containerName} --name test.txt --file /tmp/blob-list-upload.txt --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob list --container-name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                var blobs = resp.AsArray();
                Assert.That(blobs.Any(blob => blob?["name"]?.GetValue<string>() == "test.txt"), Is.True);
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task Container_ShowPermission_ReturnsEmptySignedIdentifiers()
    {
        const string storageAccountName = "topazstorcntracl01";
        const string resourceGroup = "test-storage-cntr-acl-rg";
        const string containerName = "aclcontainer";

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
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage container show-permission --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                // az storage container show-permission only returns publicAccess (its CLI transform strips signed_identifiers)
                Assert.That(resp["publicAccess"]?.GetValue<string>(), Is.EqualTo("off"));
                Assert.That(resp["signed_identifiers"], Is.Null);
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task Container_SetPermission_PersistsAndIsRetrievable()
    {
        const string storageAccountName = "topazstorcntracl02";
        const string resourceGroup = "test-storage-cntr-acl2-rg";
        const string containerName = "aclcontainer2";
        const string policyId = "read-policy";

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
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        var start = DateTime.UtcNow.AddMinutes(-1).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var expiry = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ssZ");

        await RunAzureCliCommand(
            $"az storage container policy create --container-name {containerName} --name {policyId} --permissions r --start \"{start}\" --expiry \"{expiry}\" --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        // az storage container show-permission strips signed_identifiers via its transform function;
        // use policy list which returns the stored access policies as a JSON object keyed by policy id.
        await RunAzureCliCommand(
            $"az storage container policy list --container-name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                var policies = resp.AsObject();
                Assert.That(policies, Is.Not.Null, "policy list should not be null after setting a policy");
                Assert.That(policies!, Has.Count.EqualTo(1), "Expected exactly one policy");
                Assert.That(policies!.ContainsKey(policyId), Is.True, $"Expected policy '{policyId}' to exist");
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerLease_Acquire_ReturnsLeaseId()
    {
        const string storageAccountName = "topazstorleaseacq01";
        const string resourceGroup = "test-storage-lease-acq-rg";
        const string containerName = "lease-acq-container";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage container lease acquire --container-name {containerName} --lease-duration 30 --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                var leaseId = resp.GetValue<string>();
                Assert.That(leaseId, Is.Not.Null.And.Not.Empty);
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerLease_Release_Succeeds()
    {
        const string storageAccountName = "topazstorleaserel01";
        const string resourceGroup = "test-storage-lease-rel-rg";
        const string containerName = "lease-rel-container";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        string? leaseId = null;
        await RunAzureCliCommand(
            $"az storage container lease acquire --container-name {containerName} --lease-duration 30 --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) => { leaseId = resp.GetValue<string>(); });

        await RunAzureCliCommand(
            $"az storage container lease release --container-name {containerName} --lease-id \"{leaseId}\" --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerLease_Break_ReturnsAccepted()
    {
        const string storageAccountName = "topazstorleasebrk01";
        const string resourceGroup = "test-storage-lease-brk-rg";
        const string containerName = "lease-brk-container";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage container lease acquire --container-name {containerName} --lease-duration 30 --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage container lease break --container-name {containerName} --lease-break-period 0 --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                // break returns the remaining lease time in seconds
                Assert.That(resp.GetValue<int>(), Is.GreaterThanOrEqualTo(0));
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task PageBlob_Create_AndUpload_ShouldHavePageBlobType()
    {
        const string storageAccountName = "topazpageblob01";
        const string resourceGroup = "test-page-blob-rg";
        const string containerName = "page-container";
        const string blobName = "test-page.bin";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        // Generate 512 bytes of data and upload as a page blob
        await RunAzureCliCommand(
            $"bash -c \"python3 -c \\\"import sys; sys.stdout.buffer.write(b'\\\\x41' * 512)\\\" > /tmp/topaz_page.bin && az storage blob upload --account-name {storageAccountName} --account-key \\\"{accountKey}\\\" --container-name {containerName} --name {blobName} --file /tmp/topaz_page.bin --type page --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891\"");

        await RunAzureCliCommand(
            $"az storage blob show --account-name {storageAccountName} --account-key \"{accountKey}\" --container-name {containerName} --name {blobName} --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                Assert.That(resp["properties"]!["blobType"]!.GetValue<string>(), Is.EqualTo("PageBlob"));
                Assert.That(resp["properties"]!["contentLength"]!.GetValue<long>(), Is.EqualTo(512));
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task PageBlob_UploadedContent_ShouldBeDownloadable()
    {
        const string storageAccountName = "topazpageblob02";
        const string resourceGroup = "test-page-blob-dl-rg";
        const string containerName = "page-dl-container";
        const string blobName = "download-page.bin";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"bash -c \"python3 -c \\\"import sys; sys.stdout.buffer.write(b'\\\\x42' * 512)\\\" > /tmp/topaz_page2.bin && az storage blob upload --account-name {storageAccountName} --account-key \\\"{accountKey}\\\" --container-name {containerName} --name {blobName} --file /tmp/topaz_page2.bin --type page --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891\"");

        // Download and verify content size
        await RunAzureCliCommand(
            $"bash -c \"az storage blob download --account-name {storageAccountName} --account-key \\\"{accountKey}\\\" --container-name {containerName} --name {blobName} --file /tmp/topaz_page_dl.bin --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891 -o none && wc -c < /tmp/topaz_page_dl.bin\"",
            (resp) =>
            {
                Assert.That(resp.GetValue<int>(), Is.EqualTo(512));
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task PageBlob_Show_ShouldIncludePageRanges()
    {
        const string storageAccountName = "topazpageblob03";
        const string resourceGroup = "test-page-ranges-rg";
        const string containerName = "page-ranges-container";
        const string blobName = "ranges-page.bin";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"bash -c \"python3 -c \\\"import sys; sys.stdout.buffer.write(b'\\\\x43' * 1024)\\\" > /tmp/topaz_page3.bin && az storage blob upload --account-name {storageAccountName} --account-key \\\"{accountKey}\\\" --container-name {containerName} --name {blobName} --file /tmp/topaz_page3.bin --type page --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891\"");

        await RunAzureCliCommand(
            $"az storage blob show --account-name {storageAccountName} --account-key \"{accountKey}\" --container-name {containerName} --name {blobName} --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                Assert.That(resp["properties"]!["pageRanges"]!.AsArray(), Has.Count.EqualTo(1));
                Assert.That(resp["properties"]!["pageRanges"]![0]!["start"]!.GetValue<int>(), Is.EqualTo(0));
                Assert.That(resp["properties"]!["pageRanges"]![0]!["end"]!.GetValue<int>(), Is.EqualTo(1023));
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task BlobLease_Acquire_Succeeds()
    {
        const string storageAccountName = "topazbloblease01";
        const string resourceGroup = "test-blob-lease-acq-rg";
        const string containerName = "blob-lease-acq";
        const string blobName = "lease-target.txt";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob upload --account-name {storageAccountName} --account-key \"{accountKey}\" --container-name {containerName} --name {blobName} --data \"hello\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob lease acquire --blob-name {blobName} --container-name {containerName} --lease-duration 30 --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                var leaseId = resp.GetValue<string>();
                Assert.That(leaseId, Is.Not.Null.And.Not.Empty);
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task BlobLease_Release_Succeeds()
    {
        const string storageAccountName = "topazbloblease02";
        const string resourceGroup = "test-blob-lease-rel-rg";
        const string containerName = "blob-lease-rel";
        const string blobName = "lease-release.txt";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob upload --account-name {storageAccountName} --account-key \"{accountKey}\" --container-name {containerName} --name {blobName} --data \"hello\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        string? leaseId = null;
        await RunAzureCliCommand(
            $"az storage blob lease acquire --blob-name {blobName} --container-name {containerName} --lease-duration 30 --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) => { leaseId = resp.GetValue<string>(); });

        await RunAzureCliCommand(
            $"az storage blob lease release --blob-name {blobName} --container-name {containerName} --lease-id \"{leaseId}\" --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task BlobLease_Break_ReturnsAccepted()
    {
        const string storageAccountName = "topazbloblease03";
        const string resourceGroup = "test-blob-lease-brk-rg";
        const string containerName = "blob-lease-brk";
        const string blobName = "lease-break.txt";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob upload --account-name {storageAccountName} --account-key \"{accountKey}\" --container-name {containerName} --name {blobName} --data \"hello\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob lease acquire --blob-name {blobName} --container-name {containerName} --lease-duration 30 --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob lease break --blob-name {blobName} --container-name {containerName} --lease-break-period 0 --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                Assert.That(resp.GetValue<int>(), Is.GreaterThanOrEqualTo(0));
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task BlobCopy_Start_CopiesBlobSuccessfully()
    {
        const string storageAccountName = "topazstorblobcopy01";
        const string resourceGroup = "test-blob-copy-rg";
        const string srcContainer = "blob-copy-src";
        const string dstContainer = "blob-copy-dst";
        const string srcBlobName = "source.txt";
        const string dstBlobName = "destination.txt";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage container create --name {srcContainer} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");
        await RunAzureCliCommand(
            $"az storage container create --name {dstContainer} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob upload --account-name {storageAccountName} --account-key \"{accountKey}\" --container-name {srcContainer} --name {srcBlobName} --data \"hello copy\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob copy start --source-uri http://{storageAccountName}.blob.storage.topaz.local.dev:8891/{srcContainer}/{srcBlobName} --destination-blob {dstBlobName} --destination-container {dstContainer} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                Assert.That(resp["copy_status"]?.GetValue<string>(), Is.EqualTo("success"));
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task BlobDelete_Delete_RemovesBlobSuccessfully()
    {
        const string storageAccountName = "topazstorblobdel01";
        const string resourceGroup = "test-blob-delete-rg";
        const string containerName = "blob-delete-ctr";
        const string blobName = "delete-me.txt";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob upload --account-name {storageAccountName} --account-key \"{accountKey}\" --container-name {containerName} --name {blobName} --data \"to be deleted\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob delete --container-name {containerName} --name {blobName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob exists --container-name {containerName} --name {blobName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                Assert.That(resp["exists"]!.GetValue<bool>(), Is.False);
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task BlobSnapshot_Create_ReturnsSnapshotTimestamp()
    {
        const string storageAccountName = "topazstorblobsnap01";
        const string resourceGroup = "test-blob-snapshot-rg";
        const string containerName = "blob-snapshot-ctr";
        const string blobName = "snap-target.txt";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob upload --account-name {storageAccountName} --account-key \"{accountKey}\" --container-name {containerName} --name {blobName} --data \"snapshot me\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob snapshot --container-name {containerName} --name {blobName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                var snapshot = resp["snapshot"]?.GetValue<string>();
                Assert.That(snapshot, Is.Not.Null.And.Not.Empty);
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task BlobUndelete_AfterDelete_RestoresBlobSuccessfully()
    {
        const string storageAccountName = "topazstorblobundel01";
        const string resourceGroup = "test-blob-undelete-rg";
        const string containerName = "blob-undelete-ctr";
        const string blobName = "undelete-me.txt";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage container create --name {containerName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob upload --account-name {storageAccountName} --account-key \"{accountKey}\" --container-name {containerName} --name {blobName} --data \"restore me\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob delete --container-name {containerName} --name {blobName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob exists --container-name {containerName} --name {blobName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                Assert.That(resp["exists"]!.GetValue<bool>(), Is.False);
            });

        await RunAzureCliCommand(
            $"az storage blob undelete --container-name {containerName} --name {blobName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891");

        await RunAzureCliCommand(
            $"az storage blob exists --container-name {containerName} --name {blobName} --account-name {storageAccountName} --account-key \"{accountKey}\" --blob-endpoint http://{storageAccountName}.blob.storage.topaz.local.dev:8891",
            (resp) =>
            {
                Assert.That(resp["exists"]!.GetValue<bool>(), Is.True);
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task StorageQueue_Create_SucceedsWithValidName()
    {
        const string storageAccountName = "topazstorqueuecrt01";
        const string resourceGroup = "test-queue-create-rg";
        const string queueName = "testqueue";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage queue create --name {queueName} --connection-string \"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={accountKey};QueueEndpoint=https://{storageAccountName}.queue.storage.topaz.local.dev:8893;\"",
            (resp) =>
            {
                Assert.That(resp["created"]!.GetValue<bool>(), Is.True);
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task StorageQueue_List_ReturnsCreatedQueues()
    {
        const string storageAccountName = "topazstorqueuelist01";
        const string resourceGroup = "test-queue-list-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage queue create --name queue1 --connection-string \"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={accountKey};QueueEndpoint=https://{storageAccountName}.queue.storage.topaz.local.dev:8893;\"");
        await RunAzureCliCommand(
            $"az storage queue create --name queue2 --connection-string \"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={accountKey};QueueEndpoint=https://{storageAccountName}.queue.storage.topaz.local.dev:8893;\"");

        await RunAzureCliCommand(
            $"az storage queue list --connection-string \"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={accountKey};QueueEndpoint=https://{storageAccountName}.queue.storage.topaz.local.dev:8893;\"",
            (resp) =>
            {
                var arr = resp.AsArray();
                Assert.That(arr.Any(q => q!["name"]!.GetValue<string>() == "queue1"), Is.True);
                Assert.That(arr.Any(q => q!["name"]!.GetValue<string>() == "queue2"), Is.True);
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task StorageQueue_Delete_SucceedsWhenExists()
    {
        const string storageAccountName = "topazstorqueuedel01";
        const string resourceGroup = "test-queue-delete-rg";
        const string queueName = "queue-to-delete";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage queue create --name {queueName} --connection-string \"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={accountKey};QueueEndpoint=https://{storageAccountName}.queue.storage.topaz.local.dev:8893;\"");

        await RunAzureCliCommand(
            $"az storage queue delete --name {queueName} --connection-string \"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={accountKey};QueueEndpoint=https://{storageAccountName}.queue.storage.topaz.local.dev:8893;\"",
            (resp) =>
            {
                Assert.That(resp["deleted"]!.GetValue<bool>(), Is.True);
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task StorageMessage_Put_SucceedsWithValidContent()
    {
        const string storageAccountName = "topazstorqueuemsg01";
        const string resourceGroup = "test-queue-msg-rg";
        const string queueName = "msg-test-queue";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create --name {storageAccountName} --resource-group {resourceGroup} --location westeurope --sku Standard_LRS");

        string? accountKey = null;
        await RunAzureCliCommand(
            $"az storage account keys list --account-name {storageAccountName} --resource-group {resourceGroup}",
            (resp) =>
            {
                accountKey = resp.AsArray().First(r => r!["keyName"]!.GetValue<string>() == "key1")!["value"]!.GetValue<string>();
            });

        await RunAzureCliCommand(
            $"az storage queue create --name {queueName} --connection-string \"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={accountKey};QueueEndpoint=https://{storageAccountName}.queue.storage.topaz.local.dev:8893;\"");

        await RunAzureCliCommand(
            $"az storage message put --queue-name {queueName} --content \"Hello from CLI\" --connection-string \"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={accountKey};QueueEndpoint=https://{storageAccountName}.queue.storage.topaz.local.dev:8893;\"",
            (resp) =>
            {
                Assert.That(resp["id"]!.GetValue<string>(), Is.Not.Null.And.Not.Empty);
                Assert.That(resp["popReceipt"]!.GetValue<string>(), Is.Not.Null.And.Not.Empty);
            });

        await RunAzureCliCommand($"az storage account delete --name {storageAccountName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }
}
