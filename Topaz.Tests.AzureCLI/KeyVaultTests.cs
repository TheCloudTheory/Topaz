namespace Topaz.Tests.AzureCLI;

public class KeyVaultTests : TopazFixture
{
    [Test]
    public async Task KeyVaultTests_WhenCheckNameCommandIsCalledAndKeyVaultExists_ItShouldReturnTrue()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az keyvault create --location westeurope --name MyKeyVault --resource-group test-rg");
        await RunAzureCliCommand("az keyvault check-name --name MyKeyVault");
        await RunAzureCliCommand("az keyvault delete --name MyKeyVault");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
}