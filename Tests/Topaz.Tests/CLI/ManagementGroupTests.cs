using System.Text.Json;
using Topaz.CLI;
using Topaz.Service.ManagementGroup.Models;
using Topaz.Shared;

namespace Topaz.Tests.CLI;

public class ManagementGroupTests
{
    private const string GroupId = "cli-test-mg";
    private const string GroupId2 = "cli-test-mg-2";
    private const string ChildGroupId = "cli-test-mg-child";
    private static readonly string BasePath =
        Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".management-group");

    [SetUp]
    public async Task SetUp()
    {
        // Clean up any leftover state from previous runs
        foreach (var id in new[] { GroupId, GroupId2, ChildGroupId })
        {
            await Program.RunAsync(["management-group", "delete", "--name", id]);
        }
    }

    // ── Create ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ManagementGroup_Create_ShouldPersistMetadata()
    {
        var code = await Program.RunAsync([
            "management-group", "create",
            "--name", GroupId,
            "--display-name", "CLI Test MG"
        ]);

        var metadataPath = Path.Combine(BasePath, GroupId, "metadata.json");

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(0));
            Assert.That(File.Exists(metadataPath), Is.True);
        });

        var mg = JsonSerializer.Deserialize<ManagementGroup>(
            await File.ReadAllTextAsync(metadataPath), GlobalSettings.JsonOptions);

        Assert.Multiple(() =>
        {
            Assert.That(mg, Is.Not.Null);
            Assert.That(mg!.Name, Is.EqualTo(GroupId));
            Assert.That(mg.Properties.DisplayName, Is.EqualTo("CLI Test MG"));
        });
    }

    [Test]
    public async Task ManagementGroup_Create_WithParent_ShouldLinkToParent()
    {
        await Program.RunAsync(["management-group", "create", "--name", GroupId]);

        var code = await Program.RunAsync([
            "management-group", "create",
            "--name", ChildGroupId,
            "--parent-id", $"/providers/Microsoft.Management/managementGroups/{GroupId}"
        ]);

        Assert.That(code, Is.EqualTo(0));

        var metadataPath = Path.Combine(BasePath, ChildGroupId, "metadata.json");
        var mg = JsonSerializer.Deserialize<ManagementGroup>(
            await File.ReadAllTextAsync(metadataPath), GlobalSettings.JsonOptions);

        Assert.That(mg!.Properties.Details.Parent?.Id,
            Is.EqualTo($"/providers/Microsoft.Management/managementGroups/{GroupId}"));
    }

    [Test]
    public async Task ManagementGroup_Create_WithNonExistentParent_ShouldFail()
    {
        var code = await Program.RunAsync([
            "management-group", "create",
            "--name", GroupId,
            "--parent-id", "/providers/Microsoft.Management/managementGroups/does-not-exist"
        ]);

        Assert.That(code, Is.EqualTo(1));
    }

    // ── Show ───────────────────────────────────────────────────────────────

    [Test]
    public async Task ManagementGroup_Show_WhenExists_ShouldReturnZero()
    {
        await Program.RunAsync(["management-group", "create", "--name", GroupId]);

        var code = await Program.RunAsync(["management-group", "show", "--name", GroupId]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ManagementGroup_Show_WhenNotFound_ShouldReturnOne()
    {
        var code = await Program.RunAsync(["management-group", "show", "--name", "nonexistent-mg"]);

        Assert.That(code, Is.EqualTo(1));
    }

    // ── List ───────────────────────────────────────────────────────────────

    [Test]
    public async Task ManagementGroup_List_ShouldAlwaysReturnZero()
    {
        var code = await Program.RunAsync(["management-group", "list"]);

        Assert.That(code, Is.EqualTo(0));
    }

    // ── Update ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ManagementGroup_Update_ShouldChangeDisplayName()
    {
        await Program.RunAsync(["management-group", "create", "--name", GroupId, "--display-name", "Old Name"]);

        var code = await Program.RunAsync([
            "management-group", "update",
            "--name", GroupId,
            "--display-name", "New Name"
        ]);

        Assert.That(code, Is.EqualTo(0));

        var metadataPath = Path.Combine(BasePath, GroupId, "metadata.json");
        var mg = JsonSerializer.Deserialize<ManagementGroup>(
            await File.ReadAllTextAsync(metadataPath), GlobalSettings.JsonOptions);

        Assert.That(mg!.Properties.DisplayName, Is.EqualTo("New Name"));
    }

    [Test]
    public async Task ManagementGroup_Update_WhenNotFound_ShouldReturnOne()
    {
        var code = await Program.RunAsync([
            "management-group", "update",
            "--name", "nonexistent-mg",
            "--display-name", "Whatever"
        ]);

        Assert.That(code, Is.EqualTo(1));
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ManagementGroup_Delete_ShouldRemoveMetadata()
    {
        await Program.RunAsync(["management-group", "create", "--name", GroupId]);

        var code = await Program.RunAsync(["management-group", "delete", "--name", GroupId]);

        var metadataPath = Path.Combine(BasePath, GroupId, "metadata.json");

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(0));
            Assert.That(File.Exists(metadataPath), Is.False);
        });
    }

    [Test]
    public async Task ManagementGroup_Delete_WhenNotFound_ShouldReturnOne()
    {
        var code = await Program.RunAsync(["management-group", "delete", "--name", "nonexistent-mg"]);

        Assert.That(code, Is.EqualTo(1));
    }

    // ── Subscription association ────────────────────────────────────────────

    [Test]
    public async Task ManagementGroup_SubscriptionAdd_ShouldPersistAssociation()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        await Program.RunAsync(["management-group", "create", "--name", GroupId]);

        var code = await Program.RunAsync([
            "management-group", "subscription", "add",
            "--group-id", GroupId,
            "--subscription-id", subscriptionId
        ]);

        var subPath = Path.Combine(BasePath, GroupId, "subscriptions", $"{subscriptionId}.json");

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(0));
            Assert.That(File.Exists(subPath), Is.True);
        });
    }

    [Test]
    public async Task ManagementGroup_SubscriptionShow_ShouldReturnAssociation()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        await Program.RunAsync(["management-group", "create", "--name", GroupId]);
        await Program.RunAsync([
            "management-group", "subscription", "add",
            "--group-id", GroupId, "--subscription-id", subscriptionId
        ]);

        var code = await Program.RunAsync([
            "management-group", "subscription", "show",
            "--group-id", GroupId,
            "--subscription-id", subscriptionId
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ManagementGroup_SubscriptionRemove_ShouldDeleteAssociation()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        await Program.RunAsync(["management-group", "create", "--name", GroupId]);
        await Program.RunAsync([
            "management-group", "subscription", "add",
            "--group-id", GroupId, "--subscription-id", subscriptionId
        ]);

        var code = await Program.RunAsync([
            "management-group", "subscription", "remove",
            "--group-id", GroupId,
            "--subscription-id", subscriptionId
        ]);

        var subPath = Path.Combine(BasePath, GroupId, "subscriptions", $"{subscriptionId}.json");

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(0));
            Assert.That(File.Exists(subPath), Is.False);
        });
    }

    // ── Descendants ────────────────────────────────────────────────────────

    [Test]
    public async Task ManagementGroup_DescendantsList_WhenEmpty_ShouldReturnZero()
    {
        await Program.RunAsync(["management-group", "create", "--name", GroupId]);

        var code = await Program.RunAsync([
            "management-group", "descendants", "list", "--name", GroupId
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ManagementGroup_DescendantsList_WhenNotFound_ShouldReturnOne()
    {
        var code = await Program.RunAsync([
            "management-group", "descendants", "list", "--name", "nonexistent-mg"
        ]);

        Assert.That(code, Is.EqualTo(1));
    }

    [Test]
    public async Task ManagementGroup_DescendantsList_WithChildGroupAndSubscription_ShouldReturnZero()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        await Program.RunAsync(["management-group", "create", "--name", GroupId]);
        await Program.RunAsync([
            "management-group", "create", "--name", ChildGroupId,
            "--parent-id", $"/providers/Microsoft.Management/managementGroups/{GroupId}"
        ]);
        await Program.RunAsync([
            "management-group", "subscription", "add",
            "--group-id", GroupId, "--subscription-id", subscriptionId
        ]);

        var code = await Program.RunAsync([
            "management-group", "descendants", "list", "--name", GroupId
        ]);

        Assert.That(code, Is.EqualTo(0));
    }
}
