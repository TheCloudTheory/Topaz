using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ManagementGroupDescendantsTests
{
    [Test]
    public async Task Descendants_WhenGroupNotFound_ShouldReturn404()
    {
        // Arrange
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);

        // Act / Assert
        var ex = Assert.ThrowsAsync<HttpRequestException>(
            () => topaz.GetDescendantsAsync("nonexistent-group-" + Guid.NewGuid()));

        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Descendants_WhenNoChildren_ShouldReturnEmptyList()
    {
        // Arrange
        const string groupId = "desc-empty-mg";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "Descendants Empty MG");

        // Act
        var result = await topaz.GetDescendantsAsync(groupId);
        var values = result["value"]!.AsArray();

        // Assert
        Assert.That(values, Is.Empty);
    }

    [Test]
    public async Task Descendants_WhenDirectSubscriptionAssociated_ShouldReturnSubscription()
    {
        // Arrange
        const string groupId = "desc-sub-mg";
        var subscriptionId = Guid.NewGuid().ToString();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "Descendants Sub MG");
        await topaz.AssociateSubscriptionWithManagementGroupAsync(groupId, subscriptionId);

        // Act
        var result = await topaz.GetDescendantsAsync(groupId);
        var values = result["value"]!.AsArray();

        // Assert
        var sub = values.FirstOrDefault(v => v!["name"]!.GetValue<string>() == subscriptionId);
        Assert.Multiple(() =>
        {
            Assert.That(sub, Is.Not.Null);
            Assert.That(sub!["type"]!.GetValue<string>(), Is.EqualTo("/subscriptions"));
            Assert.That(sub["id"]!.GetValue<string>(), Is.EqualTo($"/subscriptions/{subscriptionId}"));
            Assert.That(sub["properties"]!["parent"]!["id"]!.GetValue<string>(),
                Is.EqualTo($"/providers/Microsoft.Management/managementGroups/{groupId}"));
        });
    }

    [Test]
    public async Task Descendants_WhenChildGroupExists_ShouldReturnChildGroup()
    {
        // Arrange
        const string parentId = "desc-parent-mg";
        const string childId = "desc-child-mg";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(parentId, "Descendants Parent MG");
        await topaz.CreateManagementGroupWithParentAsync(childId, "Descendants Child MG", parentId);

        // Act
        var result = await topaz.GetDescendantsAsync(parentId);
        var values = result["value"]!.AsArray();

        // Assert
        var child = values.FirstOrDefault(v => v!["name"]!.GetValue<string>() == childId);
        Assert.Multiple(() =>
        {
            Assert.That(child, Is.Not.Null);
            Assert.That(child!["type"]!.GetValue<string>(),
                Is.EqualTo("Microsoft.Management/managementGroups"));
            Assert.That(child["id"]!.GetValue<string>(),
                Is.EqualTo($"/providers/Microsoft.Management/managementGroups/{childId}"));
            Assert.That(child["properties"]!["parent"]!["id"]!.GetValue<string>(),
                Is.EqualTo($"/providers/Microsoft.Management/managementGroups/{parentId}"));
        });
    }

    [Test]
    public async Task Descendants_WhenNestedHierarchy_ShouldReturnAllDescendantsRecursively()
    {
        // Arrange: root -> level1 -> level2
        const string rootId = "desc-root-mg";
        const string level1Id = "desc-level1-mg";
        const string level2Id = "desc-level2-mg";
        var subscriptionId = Guid.NewGuid().ToString();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);

        await topaz.CreateManagementGroupAsync(rootId, "Descendants Root MG");
        await topaz.CreateManagementGroupWithParentAsync(level1Id, "Descendants Level1 MG", rootId);
        await topaz.CreateManagementGroupWithParentAsync(level2Id, "Descendants Level2 MG", level1Id);
        await topaz.AssociateSubscriptionWithManagementGroupAsync(level1Id, subscriptionId);

        // Act
        var result = await topaz.GetDescendantsAsync(rootId);
        var values = result["value"]!.AsArray();
        var names = values.Select(v => v!["name"]!.GetValue<string>()).ToHashSet();

        // Assert: all descendants including nested ones
        Assert.Multiple(() =>
        {
            Assert.That(names, Does.Contain(level1Id));
            Assert.That(names, Does.Contain(level2Id));
            Assert.That(names, Does.Contain(subscriptionId));
        });
    }
}
