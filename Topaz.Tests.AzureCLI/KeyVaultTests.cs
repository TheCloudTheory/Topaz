namespace Topaz.Tests.AzureCLI;

public class KeyVaultTests : TopazFixture
{
    #region Check Name Tests
    
    [Test]
    public async Task KeyVaultTests_WhenCheckNameCommandIsCalledAndKeyVaultExists_ItShouldReturnFalse()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name MyKeyVault --resource-group test-rg");
        await RunAzureCliCommand("az keyvault check-name --name MyKeyVault", (response) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(response["nameAvailable"]!.GetValue<bool>(), Is.EqualTo(false));
                Assert.That(response["reason"]!.GetValue<string>(), Is.EqualTo("AlreadyExists"));
            });
        });
        await RunAzureCliCommand("az keyvault delete --name MyKeyVault --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenCheckNameCommandIsCalledAndKeyVaultNameIsInvalid_ItShouldNotifyTheUser()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault check-name --name MyKey--Vault", (response) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(response["nameAvailable"]!.GetValue<bool>(), Is.EqualTo(false));
                Assert.That(response["reason"]!.GetValue<string>(), Is.EqualTo("AccountNameInvalid"));
            });
        });
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenCheckNameCommandIsCalledAndNameIsAvailable_ItShouldReturnTrue()
    {
        await RunAzureCliCommand("az keyvault check-name --name AvailableKeyVaultName123", (response) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(response["nameAvailable"]!.GetValue<bool>(), Is.EqualTo(true));
                Assert.That(response["reason"], Is.Null);
            });
        });
    }
    
    [Test]
    public async Task KeyVaultTests_WhenCheckNameCommandIsCalledAndNameIsNotAvailableBecauseItWasSoftDeleted_ItShouldReturnFalse()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name MyKeyVault098 --resource-group test-rg");
        await RunAzureCliCommand("az keyvault delete --name MyKeyVault098 --only-show-errors");
        await RunAzureCliCommand("az keyvault check-name --name MyKeyVault098", (response) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(response["nameAvailable"]!.GetValue<bool>(), Is.EqualTo(false));
                Assert.That(response["reason"]!.GetValue<string>(), Is.EqualTo("AlreadyExists"));
            });
        });
    }
    
    #endregion
    
    #region Create Tests
    
    [Test]
    public async Task KeyVaultTests_WhenResourceGroupDoesNotExists_KeyVaultCannotBeCreated()
    {
        await RunAzureCliCommand("az keyvault create --location westeurope --name MyKeyVault --resource-group some-not-existing-resource-group", null, 3);
    }
    
    [Test]
    public async Task KeyVaultTests_WhenCreateCommandIsCalled_KeyVaultShouldBeCreatedWithCorrectProperties()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name TestVault123 --resource-group test-rg", (response) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("TestVault123"));
                Assert.That(response["location"]!.GetValue<string>(), Is.EqualTo("westeurope"));
                Assert.That(response["type"]!.GetValue<string>(), Is.EqualTo("Microsoft.KeyVault/vaults"));
                Assert.That(response["properties"], Is.Not.Null);
                Assert.That(response["properties"]!["tenantId"], Is.Not.Null);
                Assert.That(response["properties"]!["sku"], Is.Not.Null);
                Assert.That(response["properties"]!["sku"]!["name"]!.GetValue<string>(), Is.EqualTo("standard"));
                Assert.That(response["properties"]!["sku"]!["family"]!.GetValue<string>(), Is.EqualTo("A"));
                Assert.That(response["properties"]!["enabledForDeployment"]!.GetValue<bool>(), Is.False);
                Assert.That(response["properties"]!["enabledForDiskEncryption"]!.GetValue<bool>(), Is.False);
                Assert.That(response["properties"]!["enabledForTemplateDeployment"]!.GetValue<bool>(), Is.False);
                Assert.That(response["properties"]!["enableSoftDelete"]!.GetValue<bool>(), Is.True);
                Assert.That(response["properties"]!["softDeleteRetentionInDays"]!.GetValue<int>(), Is.EqualTo(90));
                Assert.That(response["properties"]!["enableRbacAuthorization"]!.GetValue<bool>(), Is.True);
                Assert.That(response["properties"]!["vaultUri"], Is.Not.Null);
            });
        });
        await RunAzureCliCommand("az keyvault delete --name TestVault123 --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenCreateCommandIsCalledWithEnabledForDeployment_PropertyShouldBeSet()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name TestVault456 --resource-group test-rg --enabled-for-deployment true", (response) =>
        {
            Assert.That(response["properties"]!["enabledForDeployment"]!.GetValue<bool>(), Is.True);
        });
        await RunAzureCliCommand("az keyvault delete --name TestVault456 --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenCreateCommandIsCalledWithEnabledForDiskEncryption_PropertyShouldBeSet()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name TestVault789 --resource-group test-rg --enabled-for-disk-encryption true", (response) =>
        {
            Assert.That(response["properties"]!["enabledForDiskEncryption"]!.GetValue<bool>(), Is.True);
        });
        await RunAzureCliCommand("az keyvault delete --name TestVault789 --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenCreateCommandIsCalledWithEnabledForTemplateDeployment_PropertyShouldBeSet()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name TestVaultABC --resource-group test-rg --enabled-for-template-deployment true", (response) =>
        {
            Assert.That(response["properties"]!["enabledForTemplateDeployment"]!.GetValue<bool>(), Is.True);
        });
        await RunAzureCliCommand("az keyvault delete --name TestVaultABC --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenCreateCommandIsCalledWithEnableRbacAuthorization_PropertyShouldBeSet()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name TestVaultDEF --resource-group test-rg --enable-rbac-authorization true", (response) =>
        {
            Assert.That(response["properties"]!["enableRbacAuthorization"]!.GetValue<bool>(), Is.True);
        });
        await RunAzureCliCommand("az keyvault delete --name TestVaultDEF --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenCreateCommandIsCalledWithPremiumSku_SkuShouldBeSet()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name TestVaultGHI --resource-group test-rg --sku premium", (response) =>
        {
            Assert.That(response["properties"]!["sku"]!["name"]!.GetValue<string>(), Is.EqualTo("premium"));
        });
        await RunAzureCliCommand("az keyvault delete --name TestVaultGHI --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenCreateCommandIsCalledWithRetentionDays_PropertyShouldBeSet()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name TestVaultJKL --resource-group test-rg --retention-days 7", (response) =>
        {
            Assert.That(response["properties"]!["softDeleteRetentionInDays"]!.GetValue<int>(), Is.EqualTo(7));
        });
        await RunAzureCliCommand("az keyvault delete --name TestVaultJKL --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenCreateCommandIsCalledWithTags_TagsShouldBeSet()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name TestVaultMNO --resource-group test-rg --tags Environment=Test Owner=Admin", (response) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(response["tags"], Is.Not.Null);
                Assert.That(response["tags"]!["Environment"]!.GetValue<string>(), Is.EqualTo("Test"));
                Assert.That(response["tags"]!["Owner"]!.GetValue<string>(), Is.EqualTo("Admin"));
            });
        });
        await RunAzureCliCommand("az keyvault delete --name TestVaultMNO --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    #endregion
    
    #region Show Tests
    
    [Test]
    public async Task KeyVaultTests_WhenShowCommandIsCalled_KeyVaultDetailsShouldBeReturned()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name ShowVault123 --resource-group test-rg");
        await RunAzureCliCommand("az keyvault show --name ShowVault123", (response) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("ShowVault123"));
                Assert.That(response["location"]!.GetValue<string>(), Is.EqualTo("westeurope"));
                Assert.That(response["type"]!.GetValue<string>(), Is.EqualTo("Microsoft.KeyVault/vaults"));
                Assert.That(response["id"], Is.Not.Null);
                Assert.That(response["properties"], Is.Not.Null);
            });
        });
        await RunAzureCliCommand("az keyvault delete --name ShowVault123 --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenShowCommandIsCalledWithResourceGroup_KeyVaultDetailsShouldBeReturned()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name ShowVault456 --resource-group test-rg");
        await RunAzureCliCommand("az keyvault show --name ShowVault456 --resource-group test-rg", (response) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("ShowVault456"));
                Assert.That(response["id"]!.GetValue<string>(), Does.Contain("/resourceGroups/test-rg/"));
            });
        });
        await RunAzureCliCommand("az keyvault delete --name ShowVault456 --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenShowCommandIsCalledForNonExistentVault_ItShouldFail()
    {
        await RunAzureCliCommand("az keyvault show --name NonExistentVault999", null, 1);
    }
    
    #endregion
    
    #region List Tests
    
    [Test]
    public async Task KeyVaultTests_WhenListCommandIsCalled_AllKeyVaultsShouldBeReturned()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name ListVault001 --resource-group test-rg");
        await RunAzureCliCommand("az keyvault create --location westeurope --name ListVault002 --resource-group test-rg");
        await RunAzureCliCommand("az keyvault list", (response) =>
        {
            var vaults = response.AsArray();
            Assert.That(vaults.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(vaults.Any(v => v!["name"]!.GetValue<string>() == "ListVault001"), Is.True);
            Assert.That(vaults.Any(v => v!["name"]!.GetValue<string>() == "ListVault002"), Is.True);
        });
        await RunAzureCliCommand("az keyvault delete --name ListVault001 --only-show-errors");
        await RunAzureCliCommand("az keyvault delete --name ListVault002 --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenListCommandIsCalledWithResourceGroup_OnlyVaultsInGroupShouldBeReturned()
    {
        await RunAzureCliCommand("az group create -n test-rg-1 -l westeurope");
        await RunAzureCliCommand("az group create -n test-rg-2 -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name RgVault001 --resource-group test-rg-1");
        await RunAzureCliCommand("az keyvault create --location westeurope --name RgVault002 --resource-group test-rg-2");
        await RunAzureCliCommand("az keyvault list --resource-group test-rg-1", (response) =>
        {
            var vaults = response.AsArray();
            Assert.Multiple(() =>
            {
                Assert.That(vaults.Any(v => v!["name"]!.GetValue<string>() == "RgVault001"), Is.True);
                Assert.That(vaults.Any(v => v!["name"]!.GetValue<string>() == "RgVault002"), Is.False);
            });
        });
        await RunAzureCliCommand("az keyvault delete --name RgVault001 --only-show-errors");
        await RunAzureCliCommand("az keyvault delete --name RgVault002 --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg-1 --yes");
        await RunAzureCliCommand("az group delete -n test-rg-2 --yes");
    }
    
    #endregion
    
    #region Delete Tests
    
    [Test]
    public async Task KeyVaultTests_WhenDeleteCommandIsCalled_KeyVaultShouldBeDeleted()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name DeleteVault123 --resource-group test-rg");
        await RunAzureCliCommand("az keyvault delete --name DeleteVault123 --only-show-errors");
        await RunAzureCliCommand("az keyvault show --name DeleteVault123", null, 3);
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenDeleteCommandIsCalledWithResourceGroup_KeyVaultShouldBeDeleted()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name DeleteVault456 --resource-group test-rg");
        await RunAzureCliCommand("az keyvault delete --name DeleteVault456 --resource-group test-rg --only-show-errors");
        await RunAzureCliCommand("az keyvault show --name DeleteVault456 --resource-group test-rg", null, 3);
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenDeleteCommandIsCalledForNonExistentVault_ItShouldFail()
    {
        await RunAzureCliCommand("az keyvault delete --name NonExistentVault888 --only-show-errors", null, 1);
    }
    
    #endregion
    
    #region Update Tests
    
    [Test]
    public async Task KeyVaultTests_WhenUpdateCommandIsCalledWithEnabledForDeployment_PropertyShouldBeUpdated()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name UpdateVault123 --resource-group test-rg");
        await RunAzureCliCommand("az keyvault update --name UpdateVault123 --enabled-for-deployment true", (response) =>
        {
            Assert.That(response["properties"]!["enabledForDeployment"]!.GetValue<bool>(), Is.True);
        });
        await RunAzureCliCommand("az keyvault delete --name UpdateVault123 --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenUpdateCommandIsCalledWithEnabledForDiskEncryption_PropertyShouldBeUpdated()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name UpdateVault456 --resource-group test-rg");
        await RunAzureCliCommand("az keyvault update --name UpdateVault456 --enabled-for-disk-encryption true", (response) =>
        {
            Assert.That(response["properties"]!["enabledForDiskEncryption"]!.GetValue<bool>(), Is.True);
        });
        await RunAzureCliCommand("az keyvault delete --name UpdateVault456 --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenUpdateCommandIsCalledWithEnabledForTemplateDeployment_PropertyShouldBeUpdated()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name UpdateVault789 --resource-group test-rg");
        await RunAzureCliCommand("az keyvault update --name UpdateVault789 --enabled-for-template-deployment true", (response) =>
        {
            Assert.That(response["properties"]!["enabledForTemplateDeployment"]!.GetValue<bool>(), Is.True);
        });
        await RunAzureCliCommand("az keyvault delete --name UpdateVault789 --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenUpdateCommandIsCalledWithEnableRbacAuthorization_PropertyShouldBeUpdated()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name UpdateVaultABC --resource-group test-rg");
        await RunAzureCliCommand("az keyvault update --name UpdateVaultABC --enable-rbac-authorization true", (response) =>
        {
            Assert.That(response["properties"]!["enableRbacAuthorization"]!.GetValue<bool>(), Is.True);
        });
        await RunAzureCliCommand("az keyvault delete --name UpdateVaultABC --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenUpdateCommandIsCalledWithRetentionDays_PropertyShouldBeUpdated()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name UpdateVaultDEF --resource-group test-rg");
        await RunAzureCliCommand("az keyvault update --name UpdateVaultDEF --retention-days 14", (response) =>
        {
            Assert.That(response["properties"]!["softDeleteRetentionInDays"]!.GetValue<int>(), Is.EqualTo(14));
        });
        await RunAzureCliCommand("az keyvault delete --name UpdateVaultDEF --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    #endregion
    
    #region Purge and Recovery Tests
    
    [Test]
    public async Task KeyVaultTests_WhenListDeletedCommandIsCalled_DeletedVaultsShouldBeReturned()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name DeletedVault123 --resource-group test-rg");
        await RunAzureCliCommand("az keyvault delete --name DeletedVault123 --only-show-errors");
        await RunAzureCliCommand("az keyvault list-deleted", (response) =>
        {
            var deletedVaults = response.AsArray();
            Assert.That(deletedVaults.Any(v => v!["name"]!.GetValue<string>() == "DeletedVault123"), Is.True);
        });
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenShowDeletedCommandIsCalled_DeletedVaultDetailsShouldBeReturned()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name DeletedVault456 --resource-group test-rg");
        await RunAzureCliCommand("az keyvault delete --name DeletedVault456 --only-show-errors");
        await RunAzureCliCommand("az keyvault show-deleted --name DeletedVault456", (response) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("DeletedVault456"));
                Assert.That(response["properties"], Is.Not.Null);
                Assert.That(response["properties"]!["deletionDate"], Is.Not.Null);
                Assert.That(response["properties"]!["scheduledPurgeDate"], Is.Not.Null);
            });
        });
        await RunAzureCliCommand("az keyvault purge --name DeletedVault456 --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenPurgeCommandIsCalled_DeletedVaultShouldBePermanentlyDeleted()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name PurgeVault123 --resource-group test-rg");
        await RunAzureCliCommand("az keyvault delete --name PurgeVault123 --only-show-errors");
        await RunAzureCliCommand("az keyvault purge --name PurgeVault123 --only-show-errors");
        await RunAzureCliCommand("az keyvault show-deleted --name PurgeVault123", null, 3);
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task KeyVaultTests_WhenRecoverCommandIsCalled_DeletedVaultShouldBeRecovered()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name RecoverVault123 --resource-group test-rg");
        await RunAzureCliCommand("az keyvault delete --name RecoverVault123 --only-show-errors");
        await RunAzureCliCommand("az keyvault recover --name RecoverVault123", (response) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("RecoverVault123"));
                Assert.That(response["properties"], Is.Not.Null);
            });
        });
        await RunAzureCliCommand("az keyvault show --name RecoverVault123", (response) =>
        {
            Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("RecoverVault123"));
        });
        await RunAzureCliCommand("az keyvault delete --name RecoverVault123 --only-show-errors");
        await RunAzureCliCommand("az keyvault purge --name RecoverVault123 --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    #endregion
    
    #region Wait Command Tests
    
    [Test]
    [Ignore("Do not handle wait yet.")]
    public async Task KeyVaultTests_WhenWaitCommandIsCalledForCreatedVault_ItShouldReturnSuccessfully()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name WaitVault123 --resource-group test-rg");
        await RunAzureCliCommand("az keyvault wait --name WaitVault123 --created");
        await RunAzureCliCommand("az keyvault delete --name WaitVault123 --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    [Ignore("Do not handle wait yet.")]
    public async Task KeyVaultTests_WhenWaitCommandIsCalledForDeletedVault_ItShouldReturnSuccessfully()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name WaitVault456 --resource-group test-rg");
        await RunAzureCliCommand("az keyvault delete --name WaitVault456 --only-show-errors");
        await RunAzureCliCommand("az keyvault wait --name WaitVault456 --deleted");
        await RunAzureCliCommand("az keyvault purge --name WaitVault456 --only-show-errors");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    #endregion
}