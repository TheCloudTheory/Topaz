namespace Topaz.Tests.AzureCLI;

public class VirtualNetworkTests : TopazFixture
{
    [Test]
    public async Task VirtualNetworkTests_WhenResourceGroupDoesNotExists_VirtualNetworkCannotBeCreated()
    {
        await RunAzureCliCommand("az network vnet create --location westeurope --name my-vnet --resource-group some-not-existing-resource-group", null, 3);
    }
}