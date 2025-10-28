namespace Topaz.Tests.AzureCLI;

public class KeyVaultTests : TopazFixture
{
    [Test]
    public async Task KeyVaultTests_WhenCheckNameCommandIsCalledAndKeyVaultExists_ItShouldReturnTrue()
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
    }
    
    [Test]
    public async Task KeyVaultTests_WhenResourceGroupDoesNotExists_KeyVaultCannotBeCreated()
    {
        await RunAzureCliCommand("az keyvault create --location westeurope --name MyKeyVault --resource-group some-not-existing-resource-group", null, 3);
    }
}