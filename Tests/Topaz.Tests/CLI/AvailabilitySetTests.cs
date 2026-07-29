using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class AvailabilitySetTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-0000-0000-0000-EE07000000BB");
    private const string SubscriptionName = "sub-test-avset";
    private const string ResourceGroupName = "rg-test-avset";
    private const string AvailabilitySetName = "test-cli-avset";

    private string MetadataPath => Path.Combine(
        Directory.GetCurrentDirectory(), ".topaz", ".subscription", SubscriptionId.ToString(),
        ".resource-group", ResourceGroupName, ".availability-set", AvailabilitySetName, "metadata.json");

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
            "vm",
            "availability-set", "create",
            "--name", AvailabilitySetName,
            "--resource-group", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);
    }

    [Test]
    public void AvailabilitySet_WhenCreated_MetadataFileShouldExist()
    {
        Assert.That(File.Exists(MetadataPath), Is.True);
    }

    [Test]
    public async Task AvailabilitySet_WhenRetrieved_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "vm",
            "availability-set", "show",
            "--name", AvailabilitySetName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AvailabilitySet_WhenDeleted_MetadataFileShouldNotExist()
    {
        await Program.RunAsync([
            "vm",
            "availability-set", "delete",
            "--name", AvailabilitySetName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(File.Exists(MetadataPath), Is.False);
    }

    [Test]
    public async Task AvailabilitySet_WhenUpdated_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "vm",
            "availability-set", "update",
            "--name", AvailabilitySetName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--fault-domain-count", "3"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AvailabilitySet_WhenListedByResourceGroup_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "vm",
            "availability-set", "list",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AvailabilitySet_WhenListedBySubscription_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "vm",
            "availability-set", "list-by-subscription",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AvailabilitySet_WhenAvailableSizesListed_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "vm",
            "availability-set", "list-available-sizes",
            "--name", AvailabilitySetName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }
}
