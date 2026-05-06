using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ManagementGroupEntitiesTests
{
    [Test]
    public async Task Entities_WhenNoGroupsExist_ShouldReturnEmptyList()
    {
        // Arrange
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);

        // Act
        var result = await topaz.GetEntitiesAsync();

        // Assert
        Assert.That(result["value"], Is.Not.Null);
    }

    [Test]
    public async Task Entities_WhenManagementGroupExists_ShouldReturnItInList()
    {
        // Arrange
        const string groupId = "entities-mg-test";
        const string displayName = "Entities MG Test";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, displayName);

        // Act
        var result = await topaz.GetEntitiesAsync();
        var values = result["value"]!.AsArray();

        // Assert
        var mg = values.FirstOrDefault(v =>
            v!["name"]!.GetValue<string>() == groupId);

        Assert.Multiple(() =>
        {
            Assert.That(mg, Is.Not.Null);
            Assert.That(mg!["type"]!.GetValue<string>(),
                Is.EqualTo("Microsoft.Management/managementGroups"));
            Assert.That(mg["id"]!.GetValue<string>(),
                Is.EqualTo($"/providers/Microsoft.Management/managementGroups/{groupId}"));
            Assert.That(mg["properties"]!["displayName"]!.GetValue<string>(),
                Is.EqualTo(displayName));
            Assert.That(mg["properties"]!["permissions"]!.GetValue<string>(),
                Is.EqualTo("edit"));
        });
    }

    [Test]
    public async Task Entities_WhenSubscriptionAssociated_ShouldReturnSubscriptionInList()
    {
        // Arrange
        const string groupId = "entities-sub-test";
        var subscriptionId = Guid.NewGuid().ToString();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "Entities Sub Test");
        await topaz.AssociateSubscriptionWithManagementGroupAsync(groupId, subscriptionId);

        // Act
        var result = await topaz.GetEntitiesAsync();
        var values = result["value"]!.AsArray();

        // Assert
        var sub = values.FirstOrDefault(v =>
            v!["name"]!.GetValue<string>() == subscriptionId);

        Assert.Multiple(() =>
        {
            Assert.That(sub, Is.Not.Null);
            Assert.That(sub!["type"]!.GetValue<string>(), Is.EqualTo("/subscriptions"));
            Assert.That(sub["id"]!.GetValue<string>(),
                Is.EqualTo($"/subscriptions/{subscriptionId}"));
            Assert.That(sub["properties"]!["parent"]!["id"]!.GetValue<string>(),
                Is.EqualTo($"/providers/Microsoft.Management/managementGroups/{groupId}"));
        });
    }
}
