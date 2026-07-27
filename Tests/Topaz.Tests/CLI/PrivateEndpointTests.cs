using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class PrivateEndpointTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("61A33805-CDAD-48C8-8FDA-A20A33B57FDC");
    private const string SubscriptionName = "sub-test-pe";
    private const string ResourceGroupName = "rg-test-pe";
    private const string PrivateEndpointName = "test-cli-pe";

    private static string PrivateEndpointMetadataPath => Path.Combine(
        Directory.GetCurrentDirectory(), ".topaz", ".subscription", SubscriptionId.ToString(),
        ".resource-group", ResourceGroupName, ".private-endpoint", PrivateEndpointName, "metadata.json");

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
            "vnet", "private-endpoint", "create",
            "--name", PrivateEndpointName,
            "--resource-group", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);
    }

    [Test]
    public void PrivateEndpoint_WhenPrivateEndpointIsCreated_MetadataFileShouldExist()
    {
        Assert.That(File.Exists(PrivateEndpointMetadataPath), Is.True);
    }

    [Test]
    public async Task PrivateEndpoint_WhenPrivateEndpointIsRetrieved_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "vnet", "private-endpoint", "show",
            "--name", PrivateEndpointName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task PrivateEndpoint_WhenPrivateEndpointsAreListed_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "vnet", "private-endpoint", "list",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task PrivateEndpoint_WhenPrivateEndpointsAreListedBySubscription_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "vnet", "private-endpoint", "list-by-subscription",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task PrivateEndpoint_WhenPrivateEndpointIsDeleted_MetadataFileShouldNotExist()
    {
        await Program.RunAsync([
            "vnet", "private-endpoint", "delete",
            "--name", PrivateEndpointName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(File.Exists(PrivateEndpointMetadataPath), Is.False);
    }
}
