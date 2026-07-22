using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class AppConfigurationReplicaTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("A9C8B7D6-0000-0000-0000-AC0200000000");
    private const string SubscriptionName = "sub-test-appconfig-replica";
    private const string ResourceGroupName = "rg-test-appconfig-replica";
    private const string StoreName = "test-cli-appconfig-replica";
    private const string ReplicaName = "testreplica";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);

        await Program.RunAsync([
            "subscription", "create",
            "--id", SubscriptionId.ToString(),
            "--name", SubscriptionName
        ]);

        await Program.RunAsync([
            "group", "create",
            "--name", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "appconfig", "create",
            "--name", StoreName,
            "--resource-group", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);
    }

    [Test]
    public async Task AppConfigurationReplica_WhenReplicaIsCreated_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "appconfig", "replica", "create",
            "--name", StoreName,
            "--replica-name", ReplicaName,
            "--location", "eastus",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AppConfigurationReplica_WhenReplicaIsRetrieved_CommandShouldSucceed()
    {
        await Program.RunAsync([
            "appconfig", "replica", "create",
            "--name", StoreName,
            "--replica-name", ReplicaName,
            "--location", "eastus",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "appconfig", "replica", "show",
            "--name", StoreName,
            "--replica-name", ReplicaName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AppConfigurationReplica_WhenReplicasAreListed_CommandShouldSucceed()
    {
        await Program.RunAsync([
            "appconfig", "replica", "create",
            "--name", StoreName,
            "--replica-name", ReplicaName,
            "--location", "eastus",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "appconfig", "replica", "list",
            "--name", StoreName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AppConfigurationReplica_WhenReplicaIsDeleted_CommandShouldSucceed()
    {
        await Program.RunAsync([
            "appconfig", "replica", "create",
            "--name", StoreName,
            "--replica-name", ReplicaName,
            "--location", "eastus",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "appconfig", "replica", "delete",
            "--name", StoreName,
            "--replica-name", ReplicaName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }
}
