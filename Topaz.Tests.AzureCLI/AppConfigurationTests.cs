namespace Topaz.Tests.AzureCLI;

public class AppConfigurationTests : TopazFixture
{
    private const string ResourceGroup = "rg-cli-appconfig";
    private const string StoreName = "my-cli-appconfig";
    private const int AppConfigPort = 8893;

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

    // ── Data-plane: key-value operations ─────────────────────────────────────

    [Test]
    public async Task AppConfigurationTests_WhenKeyValueIsSet_ItShouldBeRetrievable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-kv-set", null, 0);
        await RunAzureCliCommand(
            $"az appconfig create -n {StoreName}-kv-set -g {ResourceGroup}-kv-set -l westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig kv set -n {StoreName}-kv-set --key MyKey --value MyValue --yes --auth-mode login",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig kv show -n {StoreName}-kv-set --key MyKey --auth-mode login",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["key"]!.GetValue<string>(), Is.EqualTo("MyKey"));
                    Assert.That(response["value"]!.GetValue<string>(), Is.EqualTo("MyValue"));
                });
            }, 0);
    }

    [Test]
    public async Task AppConfigurationTests_WhenKeyValuesAreListed_AllShouldAppear()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-kv-list", null, 0);
        await RunAzureCliCommand(
            $"az appconfig create -n {StoreName}-kv-list -g {ResourceGroup}-kv-list -l westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig kv set -n {StoreName}-kv-list --key ListKey1 --value 1 --yes --auth-mode login",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig kv set -n {StoreName}-kv-list --key ListKey2 --value 2 --yes --auth-mode login",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig kv list --endpoint https://{StoreName}-kv-list.azconfig.topaz.local.dev:{AppConfigPort}/ --auth-mode login --all",
            response =>
            {
                var array = response.AsArray()!;
                var keys = array.Select(k => k!["key"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(keys, Does.Contain("ListKey1"));
                    Assert.That(keys, Does.Contain("ListKey2"));
                });
            }, 0);
    }

    [Test]
    public async Task AppConfigurationTests_WhenKeyValueIsDeleted_ItShouldNotBeRetrievable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-kv-del", null, 0);
        await RunAzureCliCommand(
            $"az appconfig create -n {StoreName}-kv-del -g {ResourceGroup}-kv-del -l westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig kv set -n {StoreName}-kv-del --key ToDelete --value gone --yes --auth-mode login",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig kv delete -n {StoreName}-kv-del --key ToDelete --yes --auth-mode login",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig kv show -n {StoreName}-kv-del --key ToDelete --auth-mode login",
            null, 3);
    }

    [Test]
    public async Task AppConfigurationTests_WhenKeyValueIsLocked_WritesShouldBeRejected()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-kv-lock", null, 0);
        await RunAzureCliCommand(
            $"az appconfig create -n {StoreName}-kv-lock -g {ResourceGroup}-kv-lock -l westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig kv set -n {StoreName}-kv-lock --key Immutable --value original --yes --auth-mode login",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig kv lock -n {StoreName}-kv-lock --key Immutable --yes --auth-mode login",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig kv set -n {StoreName}-kv-lock --key Immutable --value changed --yes --auth-mode login",
            null, 1);
    }

    [Test]
    public async Task AppConfigurationTests_WhenKeyValueIsUnlocked_WritesShouldSucceed()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-kv-unlock", null, 0);
        await RunAzureCliCommand(
            $"az appconfig create -n {StoreName}-kv-unlock -g {ResourceGroup}-kv-unlock -l westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig kv set -n {StoreName}-kv-unlock --key Mutable --value v1 --yes --auth-mode login",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig kv lock -n {StoreName}-kv-unlock --key Mutable --yes --auth-mode login",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig kv unlock -n {StoreName}-kv-unlock --key Mutable --yes --auth-mode login",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig kv set -n {StoreName}-kv-unlock --key Mutable --value v2 --yes --auth-mode login",
            null, 0);
        await RunAzureCliCommand(
            $"az appconfig kv show -n {StoreName}-kv-unlock --key Mutable --auth-mode login",
            response => Assert.That(response["value"]!.GetValue<string>(), Is.EqualTo("v2")), 0);
    }
}
