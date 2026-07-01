namespace Topaz.Tests.AzureCLI;

public class AppConfigurationTests : TopazFixture
{
    private const string ResourceGroup = "rg-cli-appconfig";
    private const string StoreName = "my-cli-appconfig";

    [Test]
    public async Task AppConfigurationTests_WhenStoreIsCreated_ItShouldBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}", null, 0);
        await RunAzureCliCommand(
            $"az appconfig create -n {StoreName} -g {ResourceGroup} -l westeurope",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo(StoreName));
                    Assert.That(response["type"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.AppConfiguration/configurationStores").IgnoreCase);
                    Assert.That(response["provisioningState"]!.GetValue<string>(),
                        Is.EqualTo("Succeeded"));
                });
            }, 0);
    }

    [Test]
    public async Task AppConfigurationTests_WhenStoreIsDeleted_ItShouldNotBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-del", null, 0);
        await RunAzureCliCommand(
            $"az appconfig create -n {StoreName}-del -g {ResourceGroup}-del -l westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig delete -n {StoreName}-del -g {ResourceGroup}-del --yes",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig show -n {StoreName}-del -g {ResourceGroup}-del",
            null, 3);
    }

    [Test]
    public async Task AppConfigurationTests_WhenStoresAreListed_AllShouldAppear()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-list", null, 0);
        await RunAzureCliCommand(
            $"az appconfig create -n {StoreName}-list-a -g {ResourceGroup}-list -l westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig create -n {StoreName}-list-b -g {ResourceGroup}-list -l westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig list -g {ResourceGroup}-list",
            response =>
            {
                var array = response.AsArray()!;
                var names = array.Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(names, Does.Contain($"{StoreName}-list-a"));
                    Assert.That(names, Does.Contain($"{StoreName}-list-b"));
                });
            }, 0);
    }

    [Test]
    public async Task AppConfigurationTests_WhenStoreIsUpdated_TagsShouldPersist()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-update", null, 0);
        await RunAzureCliCommand(
            $"az appconfig create -n {StoreName}-update -g {ResourceGroup}-update -l westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig update -n {StoreName}-update -g {ResourceGroup}-update --tags env=test",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig show -n {StoreName}-update -g {ResourceGroup}-update",
            response =>
            {
                Assert.That(response["tags"]!["env"]!.GetValue<string>(), Is.EqualTo("test"));
            }, 0);
    }

    [Test]
    public async Task AppConfigurationTests_WhenKeysAreListed_FourKeysShouldBeReturned()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-keys", null, 0);
        await RunAzureCliCommand(
            $"az appconfig create -n {StoreName}-keys -g {ResourceGroup}-keys -l westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig credential list -n {StoreName}-keys -g {ResourceGroup}-keys",
            response =>
            {
                var array = response.AsArray()!;
                Assert.Multiple(() =>
                {
                    Assert.That(array.Count, Is.EqualTo(4));
                    Assert.That(array.Any(k => k!["id"]!.GetValue<string>() == "Primary"), Is.True);
                    Assert.That(array.Any(k => k!["id"]!.GetValue<string>() == "Secondary"), Is.True);
                    Assert.That(array.All(k => !string.IsNullOrEmpty(k!["value"]!.GetValue<string>())), Is.True);
                });
            }, 0);
    }

    [Test]
    public async Task AppConfigurationTests_WhenKeyIsRegenerated_PrimaryKeyShouldChange()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-regen", null, 0);
        await RunAzureCliCommand(
            $"az appconfig create -n {StoreName}-regen -g {ResourceGroup}-regen -l westeurope",
            null, 0);

        string? primaryKeyBefore = null;
        await RunAzureCliCommand(
            $"az appconfig credential list -n {StoreName}-regen -g {ResourceGroup}-regen",
            response =>
            {
                var array = response.AsArray()!;
                primaryKeyBefore = array
                    .First(k => k!["id"]!.GetValue<string>() == "Primary")!["value"]!
                    .GetValue<string>();
            }, 0);

        await RunAzureCliCommand(
            $"az appconfig credential regenerate -n {StoreName}-regen -g {ResourceGroup}-regen --id Primary",
            null, 0);

        await RunAzureCliCommand(
            $"az appconfig credential list -n {StoreName}-regen -g {ResourceGroup}-regen",
            response =>
            {
                var array = response.AsArray()!;
                var primaryKeyAfter = array
                    .First(k => k!["id"]!.GetValue<string>() == "Primary")!["value"]!
                    .GetValue<string>();
                Assert.That(primaryKeyAfter, Is.Not.EqualTo(primaryKeyBefore));
            }, 0);
    }
}
