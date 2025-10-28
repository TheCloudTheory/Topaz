namespace Topaz.Tests.AzureCLI;

public class ResourceManagerTests : TopazFixture
{
    [Test]
    public async Task ResourceManagerTests_WhenDeploymentIsCreated_ItShouldBePossibleToGetIt()
    {
        await RunAzureCliCommand("az group create -n rg-deployment -l westeurope");
        await RunAzureCliCommand("az deployment group create --name test-deployment -g rg-deployment --template-file templates/empty-deployment.json");
        await RunAzureCliCommand("az deployment group show --name test-deployment -g rg-deployment", response =>
        {
            Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("test-deployment "));
        });
        await RunAzureCliCommand("az group delete -n rg-deployment --yes");
    }
}