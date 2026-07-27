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

    [Test]
    public async Task AppServiceSiteTests_WhenConfigWebIsRead_ItShouldReturnSiteConfigProperties()
    {
        await RunAzureCliCommand("az group create -n rg-webapp-config-get -l westeurope");
        await RunAzureCliCommand("az appservice plan create -n plan-webapp-config-get -g rg-webapp-config-get --sku B1 -l westeurope");
        await RunAzureCliCommand("az webapp create -n test-webapp-config-get -g rg-webapp-config-get --plan plan-webapp-config-get");
        await RunAzureCliCommand(
            "az webapp config show -n test-webapp-config-get -g rg-webapp-config-get",
            response =>
            {
                Assert.That(response["minTlsVersion"], Is.Not.Null);
            });
        await RunAzureCliCommand("az group delete -n rg-webapp-config-get --yes");
    }

    [Test]
    public async Task AppServiceSiteTests_WhenConfigWebIsUpdated_ItShouldMergeFields()
    {
        await RunAzureCliCommand("az group create -n rg-webapp-config-set -l westeurope");
        await RunAzureCliCommand("az appservice plan create -n plan-webapp-config-set -g rg-webapp-config-set --sku B1 -l westeurope");
        await RunAzureCliCommand("az webapp create -n test-webapp-config-set -g rg-webapp-config-set --plan plan-webapp-config-set");
        await RunAzureCliCommand(
            "az webapp config set -n test-webapp-config-set -g rg-webapp-config-set --always-on true",
            response =>
            {
                Assert.That(response["alwaysOn"]!.GetValue<bool>(), Is.True);
            });
        await RunAzureCliCommand("az group delete -n rg-webapp-config-set --yes");
    }

    [Test]
    public async Task AppServiceSiteTests_WhenAppSettingsAreSet_ItShouldReturnDictionary()
    {
        await RunAzureCliCommand("az group create -n rg-webapp-appsettings-set -l westeurope");
        await RunAzureCliCommand("az appservice plan create -n plan-webapp-appsettings-set -g rg-webapp-appsettings-set --sku B1 -l westeurope");
        await RunAzureCliCommand("az webapp create -n test-webapp-appsettings-set -g rg-webapp-appsettings-set --plan plan-webapp-appsettings-set");
        await RunAzureCliCommand(
            "az webapp config appsettings set -n test-webapp-appsettings-set -g rg-webapp-appsettings-set --settings MYKEY=MYVALUE",
            response =>
            {
                var settings = response.AsArray()!;
                Assert.That(settings.Any(s => s!["name"]!.GetValue<string>() == "MYKEY"), Is.True);
            });
        await RunAzureCliCommand("az group delete -n rg-webapp-appsettings-set --yes");
    }

    [Test]
    public async Task AppServiceSiteTests_WhenAppSettingsAreListed_ItShouldReturnCurrentSettings()
    {
        await RunAzureCliCommand("az group create -n rg-webapp-appsettings-list -l westeurope");
        await RunAzureCliCommand("az appservice plan create -n plan-webapp-appsettings-list -g rg-webapp-appsettings-list --sku B1 -l westeurope");
        await RunAzureCliCommand("az webapp create -n test-webapp-appsettings-list -g rg-webapp-appsettings-list --plan plan-webapp-appsettings-list");
        await RunAzureCliCommand("az webapp config appsettings set -n test-webapp-appsettings-list -g rg-webapp-appsettings-list --settings LISTKEY=LISTVALUE");
        await RunAzureCliCommand(
            "az webapp config appsettings list -n test-webapp-appsettings-list -g rg-webapp-appsettings-list",
            response =>
            {
                var settings = response.AsArray()!;
                Assert.That(settings.Any(s => s!["name"]!.GetValue<string>() == "LISTKEY" && s["value"]!.GetValue<string>() == "LISTVALUE"), Is.True);
            });
        await RunAzureCliCommand("az group delete -n rg-webapp-appsettings-list --yes");
    }
}
