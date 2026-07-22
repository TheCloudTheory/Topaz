using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppConfiguration;
using Azure.ResourceManager.AppConfiguration.Models;
using Azure.ResourceManager.Resources;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class AppConfigurationReplicaTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A9C8B7D6-2222-0000-0000-AC0200000000");

    private const string SubscriptionName = "sub-e2e-appconfig-replica";
    private const string ResourceGroupName = "rg-e2e-appconfig-replica";
    private const string StoreName = "e2e-appconfig-replica-store";
    private const string StoreNameFree = "e2e-appconfig-replica-store-free";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        await rg.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, StoreName, new AppConfigurationStoreData(AzureLocation.WestEurope, new AppConfigurationSku("Standard")));
    }

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
    }

    private ArmClient CreateClient() =>
        new(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);

    private async Task<ResourceGroupResource> GetResourceGroup(ArmClient client)
    {
        var sub = await client.GetDefaultSubscriptionAsync();
        return (await sub.GetResourceGroupAsync(ResourceGroupName)).Value;
    }

    private async Task<AppConfigurationStoreResource> GetStore(ArmClient client)
    {
        var rg = await GetResourceGroup(client);
        return (await rg.GetAppConfigurationStores().GetAsync(StoreName)).Value;
    }

    private static AppConfigurationReplicaData ReplicaData(string location = "northeurope") =>
        new() { Location = new AzureLocation(location) };

    [Test]
    public async Task AppConfigurationReplica_Create_ReplicaIsAvailable()
    {
        var client = CreateClient();
        var store = await GetStore(client);
        const string replicaName = "northreplica";

        var result = await store.GetAppConfigurationReplicas()
            .CreateOrUpdateAsync(WaitUntil.Completed, replicaName, ReplicaData());

        var replica = result.Value;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(replica.Data.Name, Is.EqualTo(replicaName));
            Assert.That(replica.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.AppConfiguration/configurationStores/replicas")));
            Assert.That(replica.Data.Location.ToString(), Is.EqualTo("northeurope").IgnoreCase);
            Assert.That(replica.Data.ProvisioningState.ToString(), Is.EqualTo("Succeeded").IgnoreCase);
        }
    }

    [Test]
    public async Task AppConfigurationReplica_Get_ReturnsReplica()
    {
        var client = CreateClient();
        var store = await GetStore(client);
        const string replicaName = "getreplica";

        await store.GetAppConfigurationReplicas()
            .CreateOrUpdateAsync(WaitUntil.Completed, replicaName, ReplicaData());

        var replica = (await store.GetAppConfigurationReplicas().GetAsync(replicaName)).Value;

        Assert.That(replica.Data.Name, Is.EqualTo(replicaName));
    }

    [Test]
    public async Task AppConfigurationReplica_Delete_ReplicaIsNotAvailableAfterDelete()
    {
        var client = CreateClient();
        var store = await GetStore(client);
        const string replicaName = "deletereplica";

        await store.GetAppConfigurationReplicas()
            .CreateOrUpdateAsync(WaitUntil.Completed, replicaName, ReplicaData());

        var replica = (await store.GetAppConfigurationReplicas().GetAsync(replicaName)).Value;
        await replica.DeleteAsync(WaitUntil.Completed);

        Assert.That(
            async () => await store.GetAppConfigurationReplicas().GetAsync(replicaName),
            Throws.InstanceOf<RequestFailedException>());
    }

    [Test]
    public async Task AppConfigurationReplica_List_AllReplicasAppear()
    {
        var client = CreateClient();
        var store = await GetStore(client);

        await store.GetAppConfigurationReplicas()
            .CreateOrUpdateAsync(WaitUntil.Completed, "listreplicaa", ReplicaData("northeurope"));
        await store.GetAppConfigurationReplicas()
            .CreateOrUpdateAsync(WaitUntil.Completed, "listreplicab", ReplicaData("eastus"));

        var replicas = new List<string>();
        await foreach (var replica in store.GetAppConfigurationReplicas().GetAllAsync())
            replicas.Add(replica.Data.Name);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(replicas, Does.Contain("listreplicaa"));
            Assert.That(replicas, Does.Contain("listreplicab"));
        }
    }

    [Test]
    public async Task AppConfigurationReplica_Create_FailsWhenNonStandardSkuSelected()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        await rg.GetAppConfigurationStores()
            .CreateOrUpdateAsync(WaitUntil.Completed, StoreNameFree, new AppConfigurationStoreData(AzureLocation.WestEurope, new AppConfigurationSku("Free")));
        var store = (await rg.GetAppConfigurationStores().GetAsync(StoreNameFree)).Value;
        const string replicaName = "freereplica";
        
        Assert.That(async () => await store.GetAppConfigurationReplicas()
            .CreateOrUpdateAsync(WaitUntil.Completed, replicaName, ReplicaData()), Throws.InstanceOf<RequestFailedException>());
    }
}
