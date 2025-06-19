namespace Topaz.Tests.AzureCLI;

public class ResourceGroupTests : TopazFixture
{
    [Test]
    public async Task ResourceGroupTests_WhenResourceGroupIsCreated_ItShouldBeAvailable()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az group list");
        await RunAzureCliCommand("az group show -n test-rg");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
}