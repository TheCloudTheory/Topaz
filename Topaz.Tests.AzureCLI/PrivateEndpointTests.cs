namespace Topaz.Tests.AzureCLI;

public class PrivateEndpointTests : TopazFixture
{
    [Test]
    public async Task PrivateEndpoint_Create_WhenResourceGroupDoesNotExist_ShouldFail()
    {
        await RunAzureCliCommand(
            "az network private-endpoint create --name pe-test --resource-group rg-not-existing --location westeurope --subnet /subscriptions/$(az account show --query id -o tsv)/resourceGroups/rg-not-existing/providers/Microsoft.Network/virtualNetworks/vnet-pe/subnets/subnet-pe --private-connection-resource-id /subscriptions/$(az account show --query id -o tsv)/resourceGroups/rg-not-existing/providers/Microsoft.Storage/storageAccounts/sa --connection-name conn1 --group-id blob",
            null, 3);
    }

    [Test]
    public async Task PrivateEndpoint_Create_ShouldSucceed()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-pe-create");
        await RunAzureCliCommand("az network vnet create --location westeurope --name vnet-pe-create --resource-group rg-pe-create --address-prefixes 10.60.0.0/16");
        await RunAzureCliCommand("az network vnet subnet create --vnet-name vnet-pe-create --name subnet-pe-create --address-prefixes 10.60.1.0/24 --resource-group rg-pe-create");
        await RunAzureCliCommand("az storage account create --name sapecreatecli --resource-group rg-pe-create --location westeurope");
        await RunAzureCliCommand(
            "az network private-endpoint create --name pe-create --resource-group rg-pe-create --location westeurope --vnet-name vnet-pe-create --subnet subnet-pe-create --private-connection-resource-id /subscriptions/$(az account show --query id -o tsv)/resourceGroups/rg-pe-create/providers/Microsoft.Storage/storageAccounts/sapecreatecli --connection-name conn-create --group-id blob",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("pe-create"));
                    Assert.That(response["location"]!.GetValue<string>(), Is.EqualTo("westeurope"));
                });
            });
    }

    [Test]
    public async Task PrivateEndpoint_Show_ShouldReturnPrivateEndpoint()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-pe-show");
        await RunAzureCliCommand("az network vnet create --location westeurope --name vnet-pe-show --resource-group rg-pe-show --address-prefixes 10.61.0.0/16");
        await RunAzureCliCommand("az network vnet subnet create --vnet-name vnet-pe-show --name subnet-pe-show --address-prefixes 10.61.1.0/24 --resource-group rg-pe-show");
        await RunAzureCliCommand("az storage account create --name sapeshowcli --resource-group rg-pe-show --location westeurope");
        await RunAzureCliCommand("az network private-endpoint create --name pe-show --resource-group rg-pe-show --location westeurope --vnet-name vnet-pe-show --subnet subnet-pe-show --private-connection-resource-id /subscriptions/$(az account show --query id -o tsv)/resourceGroups/rg-pe-show/providers/Microsoft.Storage/storageAccounts/sapeshowcli --connection-name conn-show --group-id blob");
        await RunAzureCliCommand(
            "az network private-endpoint show --name pe-show --resource-group rg-pe-show",
            response =>
            {
                Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("pe-show"));
            });
    }

    [Test]
    public async Task PrivateEndpoint_Show_WhenNotExists_ShouldFail()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-pe-show-404");
        await RunAzureCliCommand("az network private-endpoint show --name pe-not-existing --resource-group rg-pe-show-404", null, 3);
    }

    [Test]
    public async Task PrivateEndpoint_List_ByResourceGroup_ShouldReturnPrivateEndpoints()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-pe-list");
        await RunAzureCliCommand("az network vnet create --location westeurope --name vnet-pe-list --resource-group rg-pe-list --address-prefixes 10.62.0.0/16");
        await RunAzureCliCommand("az network vnet subnet create --vnet-name vnet-pe-list --name subnet-pe-list --address-prefixes 10.62.1.0/24 --resource-group rg-pe-list");
        await RunAzureCliCommand("az storage account create --name sapelistcli --resource-group rg-pe-list --location westeurope");
        await RunAzureCliCommand("az network private-endpoint create --name pe-list-a --resource-group rg-pe-list --location westeurope --vnet-name vnet-pe-list --subnet subnet-pe-list --private-connection-resource-id /subscriptions/$(az account show --query id -o tsv)/resourceGroups/rg-pe-list/providers/Microsoft.Storage/storageAccounts/sapelistcli --connection-name conn-list-a --group-id blob");
        await RunAzureCliCommand("az network private-endpoint create --name pe-list-b --resource-group rg-pe-list --location westeurope --vnet-name vnet-pe-list --subnet subnet-pe-list --private-connection-resource-id /subscriptions/$(az account show --query id -o tsv)/resourceGroups/rg-pe-list/providers/Microsoft.Storage/storageAccounts/sapelistcli --connection-name conn-list-b --group-id blob");
        await RunAzureCliCommand(
            "az network private-endpoint list --resource-group rg-pe-list",
            response =>
            {
                var names = response.AsArray().Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(names, Does.Contain("pe-list-a"));
                    Assert.That(names, Does.Contain("pe-list-b"));
                });
            });
    }

    [Test]
    public async Task PrivateEndpoint_List_BySubscription_ShouldReturnPrivateEndpoints()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-pe-list-sub");
        await RunAzureCliCommand("az network vnet create --location westeurope --name vnet-pe-list-sub --resource-group rg-pe-list-sub --address-prefixes 10.63.0.0/16");
        await RunAzureCliCommand("az network vnet subnet create --vnet-name vnet-pe-list-sub --name subnet-pe-list-sub --address-prefixes 10.63.1.0/24 --resource-group rg-pe-list-sub");
        await RunAzureCliCommand("az storage account create --name sapelistsubcli --resource-group rg-pe-list-sub --location westeurope");
        await RunAzureCliCommand("az network private-endpoint create --name pe-list-sub --resource-group rg-pe-list-sub --location westeurope --vnet-name vnet-pe-list-sub --subnet subnet-pe-list-sub --private-connection-resource-id /subscriptions/$(az account show --query id -o tsv)/resourceGroups/rg-pe-list-sub/providers/Microsoft.Storage/storageAccounts/sapelistsubcli --connection-name conn-list-sub --group-id blob");
        await RunAzureCliCommand(
            "az network private-endpoint list",
            response =>
            {
                var names = response.AsArray().Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.That(names, Does.Contain("pe-list-sub"));
            });
    }

    [Test]
    public async Task PrivateEndpoint_Delete_ShouldSucceed()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-pe-delete");
        await RunAzureCliCommand("az network vnet create --location westeurope --name vnet-pe-delete --resource-group rg-pe-delete --address-prefixes 10.64.0.0/16");
        await RunAzureCliCommand("az network vnet subnet create --vnet-name vnet-pe-delete --name subnet-pe-delete --address-prefixes 10.64.1.0/24 --resource-group rg-pe-delete");
        await RunAzureCliCommand("az storage account create --name sapedeletecli --resource-group rg-pe-delete --location westeurope");
        await RunAzureCliCommand("az network private-endpoint create --name pe-delete --resource-group rg-pe-delete --location westeurope --vnet-name vnet-pe-delete --subnet subnet-pe-delete --private-connection-resource-id /subscriptions/$(az account show --query id -o tsv)/resourceGroups/rg-pe-delete/providers/Microsoft.Storage/storageAccounts/sapedeletecli --connection-name conn-delete --group-id blob");
        await RunAzureCliCommand("az network private-endpoint delete --name pe-delete --resource-group rg-pe-delete");
        await RunAzureCliCommand("az network private-endpoint show --name pe-delete --resource-group rg-pe-delete", null, 3);
    }
}
