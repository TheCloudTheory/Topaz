using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class DiskTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("D1C2B3A4-0000-0000-0000-EE07000000AA");
    private const string SubscriptionName = "sub-test-disk";
    private const string ResourceGroupName = "rg-test-disk";
    private const string DiskName = "test-cli-disk";

    private string DiskMetadataPath => Path.Combine(
        Directory.GetCurrentDirectory(), ".topaz", ".subscription", SubscriptionId.ToString(),
        ".resource-group", ResourceGroupName, ".managed-disk", DiskName, "metadata.json");

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
            "disk", "create",
            "--name", DiskName,
            "--resource-group", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString(),
            "--disk-size-gb", "32",
            "--sku", "Premium_LRS"
        ]);
    }

    [Test]
    public void Disk_WhenDiskIsCreated_MetadataFileShouldExist()
    {
        Assert.That(File.Exists(DiskMetadataPath), Is.True);
    }

    [Test]
    public async Task Disk_WhenDiskIsRetrieved_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "disk", "show",
            "--name", DiskName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task Disk_WhenDiskIsDeleted_MetadataFileShouldNotExist()
    {
        await Program.RunAsync([
            "disk", "delete",
            "--name", DiskName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(File.Exists(DiskMetadataPath), Is.False);
    }

    [Test]
    public async Task Disk_WhenDiskIsUpdated_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "disk", "update",
            "--name", DiskName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--tags", "env=test",
            "--tags", "team=platform"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task Disk_WhenDisksAreListed_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "disk", "list",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task Disk_WhenAccessIsGranted_CommandShouldReturnAccessSasUri()
    {
        var code = await Program.RunAsync([
            "disk", "grant-access",
            "--name", DiskName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--access", "Read",
            "--duration-in-seconds", "3600"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task Disk_WhenAccessIsRevoked_CommandShouldSucceed()
    {
        await Program.RunAsync([
            "disk", "grant-access",
            "--name", DiskName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--access", "Read",
            "--duration-in-seconds", "3600"
        ]);

        var code = await Program.RunAsync([
            "disk", "revoke-access",
            "--name", DiskName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }
}
