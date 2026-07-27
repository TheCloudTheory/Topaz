using Topaz.CLI;
using Topaz.CLI.Infrastructure;
using Topaz.Shared;

namespace Topaz.Tests.CLI;

public class DefaultsCommandTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("D9E8F7A6-0000-0000-0000-BBCC00000011");
    private const string SubscriptionName = "sub-test-defaults";
    private const string ResourceGroupName = "rg-test-defaults";
    private const string Location = "westeurope";
    private const string DiskName = "disk-defaults-test";

    [SetUp]
    public async Task SetUp()
    {
        // Reset defaults to known state for each test
        await Program.RunAsync([
            "configure", "set",
            "--subscription-id", SubscriptionId.ToString(),
            "--resource-group", ResourceGroupName,
            "--location", Location
        ]);

        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync([
            "group", "create",
            "--name", ResourceGroupName,
            "--location", Location,
            "--subscription-id", SubscriptionId.ToString()
        ]);
    }

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
    }

    [Test]
    public async Task SetDefault_SubscriptionId_IsAppliedWhenFlagOmitted()
    {
        var exitCode = await Program.RunAsync([
            "disk", "create",
            "--name", DiskName,
            "--resource-group", ResourceGroupName,
            "--location", Location
        ]);

        Assert.That(exitCode, Is.EqualTo(0));
    }

    [Test]
    public async Task SetDefault_ResourceGroup_IsAppliedWhenFlagOmitted()
    {
        var exitCode = await Program.RunAsync([
            "disk", "create",
            "--name", DiskName,
            "--subscription-id", SubscriptionId.ToString(),
            "--location", Location
        ]);

        Assert.That(exitCode, Is.EqualTo(0));
    }

    [Test]
    public async Task SetDefault_Location_IsAppliedWhenFlagOmitted()
    {
        var exitCode = await Program.RunAsync([
            "disk", "create",
            "--name", DiskName,
            "--subscription-id", SubscriptionId.ToString(),
            "--resource-group", ResourceGroupName
        ]);

        Assert.That(exitCode, Is.EqualTo(0));
    }

    [Test]
    public async Task SetDefault_AllThree_AllowsCreateWithNameOnly()
    {
        var exitCode = await Program.RunAsync([
            "disk", "create",
            "--name", DiskName
        ]);

        Assert.That(exitCode, Is.EqualTo(0));
    }

    [Test]
    public async Task ExplicitFlag_OverridesDefault_WhenSubscriptionIdProvided()
    {
        // Default is SubscriptionId (set in SetUp). Pass a different subscription explicitly.
        // The disk should land in the explicit subscription's storage path, not the default.
        var explicitSubId = Guid.Parse("00000000-0000-0000-0000-CCDD00EEFF11");
        await Program.RunAsync(["subscription", "create", "--id", explicitSubId.ToString(), "--name", "sub-override"]);
        await Program.RunAsync([
            "group", "create",
            "--name", ResourceGroupName,
            "--location", Location,
            "--subscription-id", explicitSubId.ToString()
        ]);

        var exitCode = await Program.RunAsync([
            "disk", "create",
            "--name", DiskName,
            "--subscription-id", explicitSubId.ToString(),
            "--resource-group", ResourceGroupName,
            "--location", Location
        ]);

        Assert.That(exitCode, Is.EqualTo(0));

        // Verify the disk landed in the explicit subscription, not the default
        var diskInExplicitSub = Path.Combine(
            Directory.GetCurrentDirectory(), ".topaz", ".subscription", explicitSubId.ToString(),
            ".resource-group", ResourceGroupName, ".managed-disk", DiskName, "metadata.json");
        var diskInDefaultSub = Path.Combine(
            Directory.GetCurrentDirectory(), ".topaz", ".subscription", SubscriptionId.ToString(),
            ".resource-group", ResourceGroupName, ".managed-disk", DiskName, "metadata.json");

        Assert.That(File.Exists(diskInExplicitSub), Is.True, "Disk should exist in the explicitly provided subscription");
        Assert.That(File.Exists(diskInDefaultSub), Is.False, "Disk should NOT exist in the default subscription");

        await Program.RunAsync(["subscription", "delete", "--id", explicitSubId.ToString()]);
    }

    [Test]
    public async Task ShowDefaults_DisplaysCurrentValues_ExitCode0()
    {
        var exitCode = await Program.RunAsync(["configure", "show"]);

        Assert.That(exitCode, Is.EqualTo(0));
    }

    [Test]
    public async Task SetDefaults_PartialUpdate_PreservesOtherDefaults()
    {
        // SetUp already set all 3. Now update only location.
        await Program.RunAsync(["configure", "set", "--location", "eastus"]);

        // Subscription ID and resource group should still be set from SetUp — disk create with only
        // --name and no other flags should still succeed using all three preserved/updated defaults.
        var exitCode = await Program.RunAsync([
            "disk", "create",
            "--name", DiskName
            // All three flags omitted — relies on sub-id and rg from SetUp defaults, location from partial update
        ]);

        Assert.That(exitCode, Is.EqualTo(0));
    }
}
