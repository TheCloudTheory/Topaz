using Azure;
using Azure.Core;
using Azure.Data.AppConfiguration;
using Azure.ResourceManager;
using Azure.ResourceManager.AppConfiguration;
using Azure.ResourceManager.AppConfiguration.Models;
using Azure.ResourceManager.Resources;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class AppConfigurationTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A9C8B7D6-1111-0000-0000-AC0200000000");

    private const string SubscriptionName = "sub-e2e-appconfig";
    private const string ResourceGroupName = "rg-e2e-appconfig";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
    }

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
    }

    private ArmClient CreateClient() =>
        new(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);

    private static AppConfigurationStoreData MinimalStoreData() =>
        new(AzureLocation.WestEurope, new AppConfigurationSku("free"));

    private async Task<ResourceGroupResource> GetResourceGroup(ArmClient client)
    {
        var sub = await client.GetDefaultSubscriptionAsync();
        return (await sub.GetResourceGroupAsync(ResourceGroupName)).Value;
    }

    [Test]
    public async Task AppConfiguration_Create_StoreIsAvailable()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string storeName = "e2e-appconfig-create";

        var result = await rg.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, storeName, MinimalStoreData());

        var store = result.Value;
        Assert.Multiple(() =>
        {
            Assert.That(store.Data.Name, Is.EqualTo(storeName));
            Assert.That(store.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.AppConfiguration/configurationStores")));
            Assert.That(store.Data.Location.ToString(), Is.EqualTo("westeurope").IgnoreCase);
            Assert.That(store.Data.ProvisioningState.ToString(), Is.EqualTo("Succeeded").IgnoreCase);
        });
    }

    [Test]
    public async Task AppConfiguration_Get_ReturnsStore()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string storeName = "e2e-appconfig-get";

        await rg.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, storeName, MinimalStoreData());

        var store = (await rg.GetAppConfigurationStores().GetAsync(storeName)).Value;

        Assert.That(store.Data.Name, Is.EqualTo(storeName));
    }

    [Test]
    public async Task AppConfiguration_Delete_StoreIsNotAvailableAfterDelete()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string storeName = "e2e-appconfig-delete";

        await rg.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, storeName, MinimalStoreData());

        var store = (await rg.GetAppConfigurationStores().GetAsync(storeName)).Value;
        await store.DeleteAsync(WaitUntil.Completed);

        Assert.That(
            async () => await rg.GetAppConfigurationStores().GetAsync(storeName),
            Throws.InstanceOf<RequestFailedException>());
    }

    [Test]
    public async Task AppConfiguration_List_AllStoresAppear()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);

        await rg.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, "e2e-appconfig-list-a", MinimalStoreData());
        await rg.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, "e2e-appconfig-list-b", MinimalStoreData());

        var stores = new List<string>();
        await foreach (var store in rg.GetAppConfigurationStores().GetAllAsync())
            stores.Add(store.Data.Name);

        Assert.Multiple(() =>
        {
            Assert.That(stores, Does.Contain("e2e-appconfig-list-a"));
            Assert.That(stores, Does.Contain("e2e-appconfig-list-b"));
        });
    }

    [Test]
    public async Task AppConfiguration_ListKeys_ReturnsFourKeys()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string storeName = "e2e-appconfig-listkeys";

        await rg.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, storeName, MinimalStoreData());

        var store = (await rg.GetAppConfigurationStores().GetAsync(storeName)).Value;

        var keys = new List<AppConfigurationStoreApiKey>();
        await foreach (var key in store.GetKeysAsync())
            keys.Add(key);

        Assert.Multiple(() =>
        {
            Assert.That(keys, Has.Count.EqualTo(4));
            Assert.That(keys.Any(k => k.Id == "Primary" && k.IsReadOnly == false), Is.True);
            Assert.That(keys.Any(k => k.Id == "Secondary" && k.IsReadOnly == false), Is.True);
            Assert.That(keys.Any(k => k.Id == "Primary Read Only" && k.IsReadOnly == true), Is.True);
            Assert.That(keys.Any(k => k.Id == "Secondary Read Only" && k.IsReadOnly == true), Is.True);
            Assert.That(keys.All(k => !string.IsNullOrEmpty(k.Value)), Is.True);
            Assert.That(keys.All(k => k.ConnectionString!.Contains("Endpoint=")), Is.True);
        });
    }

    [Test]
    public async Task AppConfiguration_RegenerateKey_PrimaryKeyChangesOthersUnchanged()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string storeName = "e2e-appconfig-regenkey";

        await rg.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, storeName, MinimalStoreData());

        var store = (await rg.GetAppConfigurationStores().GetAsync(storeName)).Value;

        var keysBefore = new List<AppConfigurationStoreApiKey>();
        await foreach (var key in store.GetKeysAsync())
            keysBefore.Add(key);

        await store.RegenerateKeyAsync(new AppConfigurationRegenerateKeyContent { Id = "Primary" });

        var keysAfter = new List<AppConfigurationStoreApiKey>();
        await foreach (var key in store.GetKeysAsync())
            keysAfter.Add(key);

        var primaryBefore = keysBefore.Single(k => k.Id == "Primary").Value;
        var primaryAfter = keysAfter.Single(k => k.Id == "Primary").Value;
        var secondaryBefore = keysBefore.Single(k => k.Id == "Secondary").Value;
        var secondaryAfter = keysAfter.Single(k => k.Id == "Secondary").Value;

        Assert.Multiple(() =>
        {
            Assert.That(primaryAfter, Is.Not.EqualTo(primaryBefore));
            Assert.That(secondaryAfter, Is.EqualTo(secondaryBefore));
        });
    }

    private async Task<ConfigurationClient> CreateDataPlaneClient(string storeName)
    {
        var armClient = CreateClient();
        var rg = await GetResourceGroup(armClient);
        var store = (await rg.GetAppConfigurationStores().GetAsync(storeName)).Value;

        var keys = new List<AppConfigurationStoreApiKey>();
        await foreach (var key in store.GetKeysAsync())
            keys.Add(key);

        var connectionString = keys.Single(k => k.Id == "Primary").ConnectionString!;
        var options = new ConfigurationClientOptions();
        options.Retry.MaxRetries = 0;
        return new ConfigurationClient(connectionString, options);
    }

    [Test]
    public async Task AppConfiguration_DataPlane_SetAndGet_RoundTrip()
    {
        var armClient = CreateClient();
        var rg = await GetResourceGroup(armClient);
        const string storeName = "e2e-appconfig-dp-set";

        await rg.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, storeName, MinimalStoreData());

        var configClient = await CreateDataPlaneClient(storeName);
        await configClient.SetConfigurationSettingAsync(new ConfigurationSetting("MyApp:FontSize", "16"));

        var retrieved = (await configClient.GetConfigurationSettingAsync("MyApp:FontSize")).Value;

        Assert.Multiple(() =>
        {
            Assert.That(retrieved.Key, Is.EqualTo("MyApp:FontSize"));
            Assert.That(retrieved.Value, Is.EqualTo("16"));
        });
    }

    [Test]
    public async Task AppConfiguration_DataPlane_List_ContainsAllSettings()
    {
        var armClient = CreateClient();
        var rg = await GetResourceGroup(armClient);
        const string storeName = "e2e-appconfig-dp-list";

        await rg.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, storeName, MinimalStoreData());

        var configClient = await CreateDataPlaneClient(storeName);
        await configClient.SetConfigurationSettingAsync(new ConfigurationSetting("Key1", "Value1"));
        await configClient.SetConfigurationSettingAsync(new ConfigurationSetting("Key2", "Value2"));

        var settings = new List<ConfigurationSetting>();
        await foreach (var s in configClient.GetConfigurationSettingsAsync(new SettingSelector()))
            settings.Add(s);

        Assert.Multiple(() =>
        {
            Assert.That(settings.Any(s => s.Key == "Key1" && s.Value == "Value1"), Is.True);
            Assert.That(settings.Any(s => s.Key == "Key2" && s.Value == "Value2"), Is.True);
        });
    }

    [Test]
    public async Task AppConfiguration_DataPlane_Delete_SettingNotFoundAfterDelete()
    {
        var armClient = CreateClient();
        var rg = await GetResourceGroup(armClient);
        const string storeName = "e2e-appconfig-dp-delete";

        await rg.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, storeName, MinimalStoreData());

        var configClient = await CreateDataPlaneClient(storeName);
        await configClient.SetConfigurationSettingAsync(new ConfigurationSetting("ToDelete", "val"));
        await configClient.DeleteConfigurationSettingAsync("ToDelete");

        Assert.That(
            async () => await configClient.GetConfigurationSettingAsync("ToDelete"),
            Throws.InstanceOf<RequestFailedException>().With.Property("Status").EqualTo(404));
    }

    [Test]
    public async Task AppConfiguration_DataPlane_Update_ValueIsUpdated()
    {
        var armClient = CreateClient();
        var rg = await GetResourceGroup(armClient);
        const string storeName = "e2e-appconfig-dp-update";

        await rg.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, storeName, MinimalStoreData());

        var configClient = await CreateDataPlaneClient(storeName);
        await configClient.SetConfigurationSettingAsync(new ConfigurationSetting("Counter", "1"));
        await configClient.SetConfigurationSettingAsync(new ConfigurationSetting("Counter", "2"));

        var retrieved = (await configClient.GetConfigurationSettingAsync("Counter")).Value;
        Assert.That(retrieved.Value, Is.EqualTo("2"));
    }

    [Test]
    public async Task AppConfiguration_DataPlane_Label_IsolatesSettings()
    {
        var armClient = CreateClient();
        var rg = await GetResourceGroup(armClient);
        const string storeName = "e2e-appconfig-dp-label";

        await rg.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, storeName, MinimalStoreData());

        var configClient = await CreateDataPlaneClient(storeName);
        await configClient.SetConfigurationSettingAsync(new ConfigurationSetting("Env", "prod") { Label = "production" });
        await configClient.SetConfigurationSettingAsync(new ConfigurationSetting("Env", "dev") { Label = "development" });

        var prod = (await configClient.GetConfigurationSettingAsync("Env", "production")).Value;
        var dev = (await configClient.GetConfigurationSettingAsync("Env", "development")).Value;

        Assert.Multiple(() =>
        {
            Assert.That(prod.Value, Is.EqualTo("prod"));
            Assert.That(dev.Value, Is.EqualTo("dev"));
        });
    }

    [Test]
    public async Task AppConfiguration_DataPlane_SetReadOnly_PreventsMutation()
    {
        var armClient = CreateClient();
        var rg = await GetResourceGroup(armClient);
        const string storeName = "e2e-appconfig-dp-readonly";

        await rg.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, storeName, MinimalStoreData());

        var configClient = await CreateDataPlaneClient(storeName);
        await configClient.SetConfigurationSettingAsync(new ConfigurationSetting("Locked", "original"));
        await configClient.SetReadOnlyAsync("Locked", isReadOnly: true);

        Assert.That(
            async () => await configClient.SetConfigurationSettingAsync(new ConfigurationSetting("Locked", "mutated")),
            Throws.InstanceOf<RequestFailedException>().With.Property("Status").EqualTo(409));
    }

    [Test]
    public async Task AppConfiguration_DataPlane_ClearReadOnly_AllowsMutationAgain()
    {
        var armClient = CreateClient();
        var rg = await GetResourceGroup(armClient);
        const string storeName = "e2e-appconfig-dp-clearreadonly";

        await rg.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, storeName, MinimalStoreData());

        var configClient = await CreateDataPlaneClient(storeName);
        await configClient.SetConfigurationSettingAsync(new ConfigurationSetting("Locked", "original"));
        await configClient.SetReadOnlyAsync("Locked", isReadOnly: true);
        await configClient.SetReadOnlyAsync("Locked", isReadOnly: false);

        await configClient.SetConfigurationSettingAsync(new ConfigurationSetting("Locked", "mutated"));
        var retrieved = (await configClient.GetConfigurationSettingAsync("Locked")).Value;
        Assert.That(retrieved.Value, Is.EqualTo("mutated"));
    }
}
