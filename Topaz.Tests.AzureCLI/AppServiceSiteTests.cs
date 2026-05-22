namespace Topaz.Tests.AzureCLI;

public class AppServiceSiteTests : TopazFixture
{
    [Test]
    public async Task AppServiceSiteTests_WhenSiteIsCreated_ItShouldBeAvailableWithCorrectProperties()
    {
        await RunAzureCliCommand("az group create -n rg-webapp-create -l westeurope");
        await RunAzureCliCommand("az appservice plan create -n plan-webapp-create -g rg-webapp-create --sku B1 -l westeurope");
        await RunAzureCliCommand(
            "az webapp create -n test-webapp -g rg-webapp-create --plan plan-webapp-create",
            response =>
            {
                Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("test-webapp"));
                Assert.That(response["state"]!.GetValue<string>(), Is.EqualTo("Running"));
                Assert.That(response["defaultHostName"]!.GetValue<string>(), Is.EqualTo("test-webapp.azurewebsites.topaz.local.dev"));
            });
        await RunAzureCliCommand("az group delete -n rg-webapp-create --yes");
    }

    [Test]
    public async Task AppServiceSiteTests_WhenSiteIsShown_ItShouldReturnCorrectSite()
    {
        await RunAzureCliCommand("az group create -n rg-webapp-show -l westeurope");
        await RunAzureCliCommand("az appservice plan create -n plan-webapp-show -g rg-webapp-show --sku B1 -l westeurope");
        await RunAzureCliCommand("az webapp create -n test-webapp-show -g rg-webapp-show --plan plan-webapp-show");
        await RunAzureCliCommand(
            "az webapp show -n test-webapp-show -g rg-webapp-show",
            response =>
            {
                Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("test-webapp-show"));
                Assert.That(response["defaultHostName"]!.GetValue<string>(), Is.EqualTo("test-webapp-show.azurewebsites.topaz.local.dev"));
            });
        await RunAzureCliCommand("az group delete -n rg-webapp-show --yes");
    }

    [Test]
    public async Task AppServiceSiteTests_WhenSiteIsDeleted_ItShouldSucceed()
    {
        await RunAzureCliCommand("az group create -n rg-webapp-delete -l westeurope");
        await RunAzureCliCommand("az appservice plan create -n plan-webapp-delete -g rg-webapp-delete --sku B1 -l westeurope");
        await RunAzureCliCommand("az webapp create -n test-webapp-delete -g rg-webapp-delete --plan plan-webapp-delete");
        await RunAzureCliCommand("az webapp delete -n test-webapp-delete -g rg-webapp-delete");
        await RunAzureCliCommand("az group delete -n rg-webapp-delete --yes");
    }

    [Test]
    public async Task AppServiceSiteTests_WhenListingByResourceGroup_ItShouldReturnAllSites()
    {
        await RunAzureCliCommand("az group create -n rg-webapp-list -l westeurope");
        await RunAzureCliCommand("az appservice plan create -n plan-webapp-list -g rg-webapp-list --sku B1 -l westeurope");
        await RunAzureCliCommand("az webapp create -n test-webapp-list1 -g rg-webapp-list --plan plan-webapp-list");
        await RunAzureCliCommand("az webapp create -n test-webapp-list2 -g rg-webapp-list --plan plan-webapp-list");
        await RunAzureCliCommand(
            "az webapp list -g rg-webapp-list",
            response =>
            {
                Assert.That(response.AsArray()!.Count, Is.EqualTo(2));
            });
        await RunAzureCliCommand("az group delete -n rg-webapp-list --yes");
    }

    [Test]
    public async Task AppServiceSiteTests_WhenCheckingNameAvailability_ItShouldReflectActualAvailability()
    {
        var checkUrl = "https://topaz.local.dev:8899/subscriptions/$(az account show --query id -o tsv)/providers/Microsoft.Web/checknameavailability";
        var checkBody = "'{\"name\": \"test-webapp-checkname\", \"type\": \"Microsoft.Web/sites\"}'";

        await RunAzureCliCommand(
            $"az rest --method post --url \"{checkUrl}\" --body {checkBody} --headers \"Content-Type=application/json\"",
            response =>
            {
                Assert.That(response["nameAvailable"]!.GetValue<bool>(), Is.True);
            });

        await RunAzureCliCommand("az group create -n rg-webapp-checkname -l westeurope");
        await RunAzureCliCommand("az appservice plan create -n plan-webapp-checkname -g rg-webapp-checkname --sku B1 -l westeurope");
        await RunAzureCliCommand("az webapp create -n test-webapp-checkname -g rg-webapp-checkname --plan plan-webapp-checkname");

        await RunAzureCliCommand(
            $"az rest --method post --url \"{checkUrl}\" --body {checkBody} --headers \"Content-Type=application/json\"",
            response =>
            {
                Assert.That(response["nameAvailable"]!.GetValue<bool>(), Is.False);
                Assert.That(response["reason"]!.GetValue<string>(), Is.EqualTo("AlreadyExists"));
            });

        await RunAzureCliCommand("az group delete -n rg-webapp-checkname --yes");
    }
}
