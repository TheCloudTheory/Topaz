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

    [Test]
    public async Task Subnet_Create_ShouldSucceed()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-subnet-create", null, 0);
        await RunAzureCliCommand("az network vnet create --location westeurope --name vnet-subnet-create --resource-group rg-subnet-create --address-prefixes 10.50.0.0/16", null, 0);
        await RunAzureCliCommand(
            "az network vnet subnet create --vnet-name vnet-subnet-create --name my-subnet --address-prefixes 10.50.1.0/24 --resource-group rg-subnet-create",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("my-subnet"));
                    Assert.That(response["type"]!.GetValue<string>(), Does.Contain("subnets"));
                });
            }, 0);
    }

    [Test]
    public async Task Subnet_Show_ShouldReturnSubnet()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-subnet-show", null, 0);
        await RunAzureCliCommand("az network vnet create --location westeurope --name vnet-subnet-show --resource-group rg-subnet-show --address-prefixes 10.51.0.0/16", null, 0);
        await RunAzureCliCommand("az network vnet subnet create --vnet-name vnet-subnet-show --name show-subnet --address-prefixes 10.51.1.0/24 --resource-group rg-subnet-show", null, 0);
        await RunAzureCliCommand(
            "az network vnet subnet show --vnet-name vnet-subnet-show --name show-subnet --resource-group rg-subnet-show",
            response =>
            {
                Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("show-subnet"));
            }, 0);
    }

    [Test]
    public async Task Subnet_List_ShouldReturnSubnets()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-subnet-list", null, 0);
        await RunAzureCliCommand("az network vnet create --location westeurope --name vnet-subnet-list --resource-group rg-subnet-list --address-prefixes 10.52.0.0/16", null, 0);
        await RunAzureCliCommand("az network vnet subnet create --vnet-name vnet-subnet-list --name list-subnet-a --address-prefixes 10.52.1.0/24 --resource-group rg-subnet-list", null, 0);
        await RunAzureCliCommand("az network vnet subnet create --vnet-name vnet-subnet-list --name list-subnet-b --address-prefixes 10.52.2.0/24 --resource-group rg-subnet-list", null, 0);
        await RunAzureCliCommand(
            "az network vnet subnet list --vnet-name vnet-subnet-list --resource-group rg-subnet-list",
            response =>
            {
                var names = response.AsArray()!.Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(names, Does.Contain("list-subnet-a"));
                    Assert.That(names, Does.Contain("list-subnet-b"));
                });
            }, 0);
    }

    [Test]
    public async Task Subnet_Delete_ShouldSucceed()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-subnet-delete", null, 0);
        await RunAzureCliCommand("az network vnet create --location westeurope --name vnet-subnet-delete --resource-group rg-subnet-delete --address-prefixes 10.53.0.0/16", null, 0);
        await RunAzureCliCommand("az network vnet subnet create --vnet-name vnet-subnet-delete --name del-subnet --address-prefixes 10.53.1.0/24 --resource-group rg-subnet-delete", null, 0);
        await RunAzureCliCommand("az network vnet subnet delete --vnet-name vnet-subnet-delete --name del-subnet --resource-group rg-subnet-delete", null, 0);
        await RunAzureCliCommand("az network vnet subnet show --vnet-name vnet-subnet-delete --name del-subnet --resource-group rg-subnet-delete", null, 3);
    }

    [Test]
    public async Task VirtualNetworkTests_CheckIpAddressAvailability_WhenIpInSubnet_ReturnsAvailable()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-checkip", null, 0);
        await RunAzureCliCommand("az network vnet create --location westeurope --name vnet-checkip --resource-group rg-checkip --address-prefixes 10.55.0.0/16", null, 0);
        await RunAzureCliCommand("az network vnet subnet create --vnet-name vnet-checkip --name checkip-subnet --address-prefixes 10.55.1.0/24 --resource-group rg-checkip", null, 0);
        await RunAzureCliCommand(
            "az network vnet check-ip-address --resource-group rg-checkip --name vnet-checkip --ip-address 10.55.1.5",
            response =>
            {
                Assert.That(response["available"]!.GetValue<bool>(), Is.True);
            }, 0);
    }

    [Test]
    public async Task VirtualNetworkTests_CheckIpAddressAvailability_WhenIpOutsideSubnet_ReturnsNotAvailable()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-checkip2", null, 0);
        await RunAzureCliCommand("az network vnet create --location westeurope --name vnet-checkip2 --resource-group rg-checkip2 --address-prefixes 10.56.0.0/16", null, 0);
        await RunAzureCliCommand("az network vnet subnet create --vnet-name vnet-checkip2 --name checkip-subnet2 --address-prefixes 10.56.1.0/24 --resource-group rg-checkip2", null, 0);
        await RunAzureCliCommand(
            "az network vnet check-ip-address --resource-group rg-checkip2 --name vnet-checkip2 --ip-address 192.168.0.1",
            response =>
            {
                Assert.That(response["available"]!.GetValue<bool>(), Is.False);
            }, 0);
    }

    [Test]
    public async Task VirtualNetwork_Delete_ShouldSucceed()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-vnet-delete", null, 0);
        await RunAzureCliCommand("az network vnet create --location westeurope --name vnet-to-delete --resource-group rg-vnet-delete", null, 0);
        await RunAzureCliCommand("az network vnet delete --name vnet-to-delete --resource-group rg-vnet-delete", null, 0);
        await RunAzureCliCommand("az network vnet show --name vnet-to-delete --resource-group rg-vnet-delete", null, 3);
    }

    [Test]
    public async Task VirtualNetwork_List_ShouldReturnVirtualNetworks()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-vnet-list", null, 0);
        await RunAzureCliCommand("az network vnet create --location westeurope --name vnet-list-a --resource-group rg-vnet-list", null, 0);
        await RunAzureCliCommand("az network vnet create --location westeurope --name vnet-list-b --resource-group rg-vnet-list", null, 0);
        await RunAzureCliCommand(
            "az network vnet list --resource-group rg-vnet-list",
            response =>
            {
                var names = response.AsArray()!.Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(names, Does.Contain("vnet-list-a"));
                    Assert.That(names, Does.Contain("vnet-list-b"));
                });
            }, 0);
    }

    [Test]
    public async Task VirtualNetwork_UpdateTags_ShouldUpdateTags()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-vnet-tags", null, 0);
        await RunAzureCliCommand("az network vnet create --location westeurope --name vnet-tags-test --resource-group rg-vnet-tags", null, 0);
        await RunAzureCliCommand(
            "az network vnet update --name vnet-tags-test --resource-group rg-vnet-tags --set tags.env=test tags.owner=topaz",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["tags"]!["env"]!.GetValue<string>(), Is.EqualTo("test"));
                    Assert.That(response["tags"]!["owner"]!.GetValue<string>(), Is.EqualTo("topaz"));
                });
            }, 0);
    }

    [Test]
    public async Task VirtualNetwork_CheckIpAddressAvailability_WhenIpAllocatedToNIC_ReturnsNotAvailable()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-nic-checkip", null, 0);
        await RunAzureCliCommand("az network vnet create --location westeurope --name vnet-nic-checkip --resource-group rg-nic-checkip --address-prefixes 10.80.0.0/16", null, 0);
        await RunAzureCliCommand("az network vnet subnet create --vnet-name vnet-nic-checkip --name subnet-nic-checkip --address-prefixes 10.80.1.0/24 --resource-group rg-nic-checkip", null, 0);
        await RunAzureCliCommand("az network nic create -n nic-checkip-cli -g rg-nic-checkip --vnet-name vnet-nic-checkip --subnet subnet-nic-checkip --private-ip-address 10.80.1.10", null, 0);
        await RunAzureCliCommand(
            "az network vnet check-ip-address-space --resource-group rg-nic-checkip --vnet-name vnet-nic-checkip --ip-address 10.80.1.10",
            response =>
            {
                Assert.That(response["available"]!.GetValue<bool>(), Is.False);
            }, 0);
    }
}