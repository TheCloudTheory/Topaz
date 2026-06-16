namespace Topaz.Tests.AzureCLI;

public class LoadBalancerTests : TopazFixture
{
    private const string ResourceGroup = "rg-cli-lb";
    private const string LoadBalancerName = "my-cli-lb";

    [Test]
    public async Task LoadBalancerTests_WhenLoadBalancerIsCreated_ItShouldBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}", null, 0);
        await RunAzureCliCommand(
            $"az network lb create --location westeurope --name {LoadBalancerName} " +
            $"--resource-group {ResourceGroup} --sku Standard",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["loadBalancer"]!["name"]!.GetValue<string>(), Is.EqualTo(LoadBalancerName));
                    Assert.That(response["loadBalancer"]!["location"]!.GetValue<string>(), Is.EqualTo("westeurope"));
                });
            }, 0);
    }

    [Test]
    public async Task LoadBalancerTests_WhenLoadBalancerIsDeleted_ItShouldNotBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-del", null, 0);
        await RunAzureCliCommand(
            $"az network lb create --location westeurope --name {LoadBalancerName}-del " +
            $"--resource-group {ResourceGroup}-del --sku Standard",
            null, 0);
        await RunAzureCliCommand(
            $"az network lb delete --resource-group {ResourceGroup}-del --name {LoadBalancerName}-del",
            null, 0);
        await RunAzureCliCommand(
            $"az network lb show --resource-group {ResourceGroup}-del --name {LoadBalancerName}-del",
            null, 3);
    }

    [Test]
    public async Task LoadBalancerTests_WhenLoadBalancersAreListed_AllShouldAppear()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-list", null, 0);
        await RunAzureCliCommand(
            $"az network lb create --location westeurope --name {LoadBalancerName}-list-a " +
            $"--resource-group {ResourceGroup}-list --sku Standard",
            null, 0);
        await RunAzureCliCommand(
            $"az network lb create --location westeurope --name {LoadBalancerName}-list-b " +
            $"--resource-group {ResourceGroup}-list --sku Standard",
            null, 0);
        await RunAzureCliCommand(
            $"az network lb list --resource-group {ResourceGroup}-list",
            response =>
            {
                var array = response.AsArray()!;
                var names = array.Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(names, Does.Contain($"{LoadBalancerName}-list-a"));
                    Assert.That(names, Does.Contain($"{LoadBalancerName}-list-b"));
                });
            }, 0);
    }

    [Test]
    public async Task LoadBalancerTests_WhenLoadBalancerIsUpdatedWithPatch_TagsShouldPersist()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-patch", null, 0);
        await RunAzureCliCommand(
            $"az network lb create --location westeurope --name {LoadBalancerName}-patch " +
            $"--resource-group {ResourceGroup}-patch --sku Standard",
            null, 0);
        await RunAzureCliCommand(
            $"az resource patch --resource-type Microsoft.Network/loadBalancers --api-version 2024-03-01 " +
            $"--resource-group {ResourceGroup}-patch --name {LoadBalancerName}-patch " +
            $"--is-full-object --properties \"{{\\\"tags\\\": {{\\\"env\\\": \\\"test\\\", \\\"team\\\": \\\"platform\\\"}}}}\"",
            null, 0);
        await RunAzureCliCommand(
            $"az network lb show --resource-group {ResourceGroup}-patch --name {LoadBalancerName}-patch",
            response =>
            {
                Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo($"{LoadBalancerName}-patch"));
                Assert.That(response["tags"]!["env"]!.GetValue<string>(), Is.EqualTo("test"));
                Assert.That(response["tags"]!["team"]!.GetValue<string>(), Is.EqualTo("platform"));
            }, 0);
    }

    [Test]
    public async Task LoadBalancerTests_WhenLoadBalancerIsShown_AllPropertiesShouldBeReturned()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-show", null, 0);
        await RunAzureCliCommand(
            $"az network lb create --location westeurope --name {LoadBalancerName}-show " +
            $"--resource-group {ResourceGroup}-show --sku Basic",
            null, 0);
        await RunAzureCliCommand(
            $"az network lb show --resource-group {ResourceGroup}-show --name {LoadBalancerName}-show",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo($"{LoadBalancerName}-show"));
                    Assert.That(response["type"]!.GetValue<string>(), Is.EqualTo("Microsoft.Network/loadBalancers").IgnoreCase);
                    Assert.That(response["sku"]!["name"]!.GetValue<string>(), Is.EqualTo("Basic"));
                });
            }, 0);
    }
}
