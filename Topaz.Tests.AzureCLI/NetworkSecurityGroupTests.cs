namespace Topaz.Tests.AzureCLI;

public class NetworkSecurityGroupTests : TopazFixture
{
    [Test]
    public async Task NetworkSecurityGroup_Create_ShouldSucceed()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-nsg-create", null, 0);
        await RunAzureCliCommand(
            "az network nsg create --location westeurope --name my-nsg --resource-group rg-nsg-create",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["NewNSG"]!["name"]!.GetValue<string>(), Is.EqualTo("my-nsg"));
                    Assert.That(response["NewNSG"]!["location"]!.GetValue<string>(), Is.EqualTo("westeurope"));
                    Assert.That(response["NewNSG"]!["type"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.Network/networkSecurityGroups"));
                });
            }, 0);
    }

    [Test]
    public async Task NetworkSecurityGroup_Show_ShouldReturnNsg()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-nsg-show", null, 0);
        await RunAzureCliCommand("az network nsg create --location westeurope --name show-nsg --resource-group rg-nsg-show", null, 0);
        await RunAzureCliCommand(
            "az network nsg show --name show-nsg --resource-group rg-nsg-show",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("show-nsg"));
                    Assert.That(response["type"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.Network/networkSecurityGroups"));
                });
            }, 0);
    }

    [Test]
    public async Task NetworkSecurityGroup_Delete_ShouldSucceed()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-nsg-delete", null, 0);
        await RunAzureCliCommand("az network nsg create --location westeurope --name del-nsg --resource-group rg-nsg-delete", null, 0);
        await RunAzureCliCommand("az network nsg delete --name del-nsg --resource-group rg-nsg-delete", null, 0);
        await RunAzureCliCommand("az network nsg show --name del-nsg --resource-group rg-nsg-delete", null, 3);
    }

    [Test]
    public async Task NetworkSecurityGroup_List_ShouldReturnNsgs()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-nsg-list", null, 0);
        await RunAzureCliCommand("az network nsg create --location westeurope --name list-nsg-a --resource-group rg-nsg-list", null, 0);
        await RunAzureCliCommand("az network nsg create --location westeurope --name list-nsg-b --resource-group rg-nsg-list", null, 0);
        await RunAzureCliCommand(
            "az network nsg list --resource-group rg-nsg-list",
            response =>
            {
                var names = response.AsArray()!.Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(names, Does.Contain("list-nsg-a"));
                    Assert.That(names, Does.Contain("list-nsg-b"));
                });
            }, 0);
    }

    [Test]
    public async Task NetworkSecurityGroup_UpdateTags_ShouldUpdateTags()
    {
        await RunAzureCliCommand("az group create -l westeurope -n rg-nsg-tags", null, 0);
        await RunAzureCliCommand("az network nsg create --location westeurope --name tags-nsg --resource-group rg-nsg-tags", null, 0);
        await RunAzureCliCommand(
            "az network nsg update --name tags-nsg --resource-group rg-nsg-tags --set tags.env=topaz tags.owner=test",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["tags"]!["env"]!.GetValue<string>(), Is.EqualTo("topaz"));
                    Assert.That(response["tags"]!["owner"]!.GetValue<string>(), Is.EqualTo("test"));
                });
            }, 0);
    }
}
