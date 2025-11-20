namespace Topaz.Tests.AzureCLI;

public class VirtualNetworkTests : TopazFixture
{
    [Test]
    public async Task VirtualNetworkTests_WhenResourceGroupDoesNotExists_VirtualNetworkCannotBeCreated()
    {
        await RunAzureCliCommand("az network vnet create --location westeurope --name my-vnet --resource-group some-not-existing-resource-group", null, 3);
    }
    
    [Test]
    public async Task VirtualNetworkTests_WhenResourceGroupExists_VirtualNetworkMustBeCreated()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-vnet", null, 0);
        await RunAzureCliCommand("az network vnet create --location westeurope --name my-vnet --resource-group rg-vnet",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["newVNet"]!["name"]!.GetValue<string>(), Is.EqualTo("my-vnet"));
                    Assert.That(response["newVNet"]!["location"]!.GetValue<string>(), Is.EqualTo("westeurope"));
                });
            }, 0);
    }
}