using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class LoadBalancerTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("C3D4E5F6-0000-0000-0000-AA11000000BB");
    private const string SubscriptionName = "sub-test-lb";
    private const string ResourceGroupName = "rg-test-lb";
    private const string LoadBalancerName = "test-cli-lb";

    private string LoadBalancerMetadataPath => Path.Combine(
        Directory.GetCurrentDirectory(), ".topaz", ".subscription", SubscriptionId.ToString(),
        ".resource-group", ResourceGroupName, ".load-balancer", LoadBalancerName, "metadata.json");

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
            "lb", "create",
            "--name", LoadBalancerName,
            "--resource-group", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString(),
            "--sku", "Standard"
        ]);
    }

    [Test]
    public void LoadBalancer_WhenLoadBalancerIsCreated_MetadataFileShouldExist()
    {
        Assert.That(File.Exists(LoadBalancerMetadataPath), Is.True);
    }

    [Test]
    public async Task LoadBalancer_WhenLoadBalancerIsRetrieved_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "lb", "show",
            "--name", LoadBalancerName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task LoadBalancer_WhenLoadBalancerIsDeleted_MetadataFileShouldNotExist()
    {
        await Program.RunAsync([
            "lb", "delete",
            "--name", LoadBalancerName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(File.Exists(LoadBalancerMetadataPath), Is.False);
    }

    [Test]
    public async Task LoadBalancer_WhenLoadBalancerTagsAreUpdated_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "lb", "update",
            "--name", LoadBalancerName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--tags", "env=test",
            "--tags", "team=platform"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task LoadBalancer_WhenLoadBalancersAreListed_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "lb", "list",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }
}
