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
    
    [Test]
    public async Task ResourceGroupTests_WhenResourceGroupIsCreatedWithProvidedLocation_TheLocationShouldBeCorrect()
    {
        await RunAzureCliCommand("az group create -n test-rg -l northeurope");
        await RunAzureCliCommand("az group show -n test-rg", (response) =>
        {
            Assert.That(response["location"]!.GetValue<string>(), Is.EqualTo("northeurope"));
        });
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }

    [Test]
    public async Task ResourceGroupTests_WhenResourceGroupIsCreatedAndChecked_ItShouldExist()
    {
        await RunAzureCliCommand("az group create -n test-exists-rg -l westeurope");
        await RunAzureCliCommand("az group exists -n test-exists-rg", (response) =>
        {
            Assert.That(response.GetValue<bool>(), Is.True);
        });
        await RunAzureCliCommand("az group delete -n test-exists-rg --yes");
    }

    [Test]
    public async Task ResourceGroupTests_WhenResourceGroupDoesNotExist_CheckShouldReturnFalse()
    {
        await RunAzureCliCommand("az group exists -n non-existing-rg", (response) =>
        {
            Assert.That(response.GetValue<bool>(), Is.False);
        });
    }
}