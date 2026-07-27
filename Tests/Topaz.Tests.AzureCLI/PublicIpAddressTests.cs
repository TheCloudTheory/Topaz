namespace Topaz.Tests.AzureCLI;

public class PublicIpAddressTests : TopazFixture
{
    private const string ResourceGroup = "rg-cli-pip";
    private const string PipName = "my-cli-pip";

    [Test]
    public async Task PublicIpAddressTests_WhenPublicIPIsCreated_ItShouldBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}", null, 0);
        await RunAzureCliCommand(
            $"az network public-ip create --name {PipName} --resource-group {ResourceGroup} --allocation-method Dynamic",
            response =>
            {
                var pip = response["publicIp"] ?? response;
                Assert.Multiple(() =>
                {
                    Assert.That(pip["name"]!.GetValue<string>(), Is.EqualTo(PipName));
                    Assert.That(pip["type"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.Network/publicIPAddresses").IgnoreCase);
                    Assert.That(pip["provisioningState"]!.GetValue<string>(),
                        Is.EqualTo("Succeeded"));
                });
            }, 0);
    }

    [Test]
    public async Task PublicIpAddressTests_WhenPublicIPIsDeleted_ItShouldNotBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-del", null, 0);
        await RunAzureCliCommand(
            $"az network public-ip create --name {PipName}-del --resource-group {ResourceGroup}-del --allocation-method Dynamic",
            null, 0);
        await RunAzureCliCommand(
            $"az network public-ip delete --name {PipName}-del --resource-group {ResourceGroup}-del",
            null, 0);
        await RunAzureCliCommand(
            $"az network public-ip show --name {PipName}-del --resource-group {ResourceGroup}-del",
            null, 3);
    }

    [Test]
    public async Task PublicIpAddressTests_WhenPublicIPsAreListed_AllShouldAppear()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-list", null, 0);
        await RunAzureCliCommand(
            $"az network public-ip create --name {PipName}-list-a --resource-group {ResourceGroup}-list --allocation-method Dynamic",
            null, 0);
        await RunAzureCliCommand(
            $"az network public-ip create --name {PipName}-list-b --resource-group {ResourceGroup}-list --allocation-method Dynamic",
            null, 0);
        await RunAzureCliCommand(
            $"az network public-ip list --resource-group {ResourceGroup}-list",
            response =>
            {
                var array = response.AsArray()!;
                var names = array.Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(names, Does.Contain($"{PipName}-list-a"));
                    Assert.That(names, Does.Contain($"{PipName}-list-b"));
                });
            }, 0);
    }
}
