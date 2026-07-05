using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ManagementGroupExpandChildrenTests
{
    [Test]
    public async Task ExpandChildren_WhenNoChildren_ShouldReturnEmptyChildrenArray()
    {
        // Arrange
        const string groupId = "mg-expand-empty";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "MG Expand Empty");

        // Act
        var result = await topaz.GetManagementGroupWithChildrenAsync(groupId);
        var children = result["properties"]!["children"]!.AsArray();

        // Assert
        Assert.That(children, Is.Empty);
    }

    [Test]
    public async Task ExpandChildren_WhenChildGroupExists_ShouldReturnChildInArray()
    {
        // Arrange
        const string parentId = "mg-expand-parent";
        const string childId = "mg-expand-child";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(parentId, "MG Expand Parent");
        await topaz.CreateManagementGroupWithParentAsync(childId, "MG Expand Child", parentId);

        // Act
        var result = await topaz.GetManagementGroupWithChildrenAsync(parentId);
        var children = result["properties"]!["children"]!.AsArray();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(children, Has.Count.EqualTo(1));
            Assert.That(children[0]!["name"]!.GetValue<string>(), Is.EqualTo(childId));
            Assert.That(children[0]!["type"]!.GetValue<string>(),
                Is.EqualTo("Microsoft.Management/managementGroups"));
        });
    }

    [Test]
    public async Task ExpandChildren_WhenMultipleChildGroupsExist_ShouldReturnAllChildren()
    {
        // Arrange
        const string parentId = "mg-expand-multi-parent";
        const string child1Id = "mg-expand-multi-child1";
        const string child2Id = "mg-expand-multi-child2";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(parentId, "MG Expand Multi Parent");
        await topaz.CreateManagementGroupWithParentAsync(child1Id, "MG Expand Child 1", parentId);
        await topaz.CreateManagementGroupWithParentAsync(child2Id, "MG Expand Child 2", parentId);

        // Act
        var result = await topaz.GetManagementGroupWithChildrenAsync(parentId);
        var children = result["properties"]!["children"]!.AsArray();
        var childNames = children.Select(c => c!["name"]!.GetValue<string>()).ToArray();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(children, Has.Count.EqualTo(2));
            Assert.That(childNames, Does.Contain(child1Id));
            Assert.That(childNames, Does.Contain(child2Id));
        });
    }

    [Test]
    public async Task WithoutExpand_ShouldNotReturnChildren()
    {
        // Arrange
        const string parentId = "mg-no-expand-parent";
        const string childId = "mg-no-expand-child";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(parentId, "MG No Expand Parent");
        await topaz.CreateManagementGroupWithParentAsync(childId, "MG No Expand Child", parentId);

        // Act
        var result = await topaz.GetManagementGroupAsync(parentId);

        // Assert
        Assert.That(result["properties"]!["children"], Is.Null);
    }
}
