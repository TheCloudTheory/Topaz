namespace Topaz.Tests.AzureCLI;

public class AppServicePlanTests : TopazFixture
{
    [Test]
    public async Task AppServicePlanTests_WhenPlanIsCreated_ItShouldBeAvailableWithCorrectProperties()
    {
        await RunAzureCliCommand("az group create -n rg-appservice-create -l westeurope");
        await RunAzureCliCommand(
            "az appservice plan create -n test-plan -g rg-appservice-create --sku B1 -l westeurope",
            response =>
            {
                Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("test-plan"));
                Assert.That(response["sku"]!["name"]!.GetValue<string>(), Is.EqualTo("B1"));
                Assert.That(response["properties"]!["provisioningState"]!.GetValue<string>(), Is.EqualTo("Succeeded"));
            });
        await RunAzureCliCommand("az group delete -n rg-appservice-create --yes");
    }

    [Test]
    public async Task AppServicePlanTests_WhenPlanIsShown_ItShouldReturnCorrectPlan()
    {
        await RunAzureCliCommand("az group create -n rg-appservice-show -l westeurope");
        await RunAzureCliCommand("az appservice plan create -n test-plan-show -g rg-appservice-show --sku S1 -l westeurope");
        await RunAzureCliCommand(
            "az appservice plan show -n test-plan-show -g rg-appservice-show",
            response =>
            {
                Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("test-plan-show"));
                Assert.That(response["sku"]!["name"]!.GetValue<string>(), Is.EqualTo("S1"));
            });
        await RunAzureCliCommand("az group delete -n rg-appservice-show --yes");
    }

    [Test]
    public async Task AppServicePlanTests_WhenPlanIsDeleted_ItShouldSucceed()
    {
        await RunAzureCliCommand("az group create -n rg-appservice-delete -l westeurope");
        await RunAzureCliCommand("az appservice plan create -n test-plan-delete -g rg-appservice-delete --sku B1 -l westeurope");
        await RunAzureCliCommand("az appservice plan delete -n test-plan-delete -g rg-appservice-delete --yes");
        await RunAzureCliCommand("az group delete -n rg-appservice-delete --yes");
    }

    [Test]
    public async Task AppServicePlanTests_WhenListingPlansByResourceGroup_ItShouldReturnAllPlans()
    {
        await RunAzureCliCommand("az group create -n rg-appservice-list -l westeurope");
        await RunAzureCliCommand("az appservice plan create -n test-plan-list1 -g rg-appservice-list --sku B1 -l westeurope");
        await RunAzureCliCommand("az appservice plan create -n test-plan-list2 -g rg-appservice-list --sku B1 -l westeurope");
        await RunAzureCliCommand(
            "az appservice plan list -g rg-appservice-list",
            response =>
            {
                Assert.That(response.AsArray()!.Count, Is.EqualTo(2));
            });
        await RunAzureCliCommand("az group delete -n rg-appservice-list --yes");
    }
}
