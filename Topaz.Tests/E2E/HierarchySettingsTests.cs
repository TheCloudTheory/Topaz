using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class HierarchySettingsTests
{
    [Test]
    public async Task HierarchySettings_WhenCreated_ShouldReturnSettings()
    {
        // Arrange
        const string groupId = "hs-create-mg";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "HS Create MG");

        // Act
        var result = await topaz.CreateOrUpdateHierarchySettingsAsync(groupId,
            requireAuthorizationForGroupCreation: true);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result["name"]!.GetValue<string>(), Is.EqualTo("default"));
            Assert.That(result["type"]!.GetValue<string>(),
                Is.EqualTo("Microsoft.Management/managementGroups/settings"));
            Assert.That(result["id"]!.GetValue<string>(),
                Is.EqualTo($"/providers/Microsoft.Management/managementGroups/{groupId}/settings/default"));
            Assert.That(result["properties"]!["requireAuthorizationForGroupCreation"]!.GetValue<bool>(),
                Is.True);
        });
    }

    [Test]
    public async Task HierarchySettings_WhenCreated_GetShouldReturnIt()
    {
        // Arrange
        const string groupId = "hs-get-mg";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "HS Get MG");
        await topaz.CreateOrUpdateHierarchySettingsAsync(groupId,
            requireAuthorizationForGroupCreation: true);

        // Act
        var result = await topaz.GetHierarchySettingsAsync(groupId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result["name"]!.GetValue<string>(), Is.EqualTo("default"));
            Assert.That(result["properties"]!["requireAuthorizationForGroupCreation"]!.GetValue<bool>(),
                Is.True);
        });
    }

    [Test]
    public async Task HierarchySettings_WhenCreated_ListShouldReturnIt()
    {
        // Arrange
        const string groupId = "hs-list-mg";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "HS List MG");
        await topaz.CreateOrUpdateHierarchySettingsAsync(groupId);

        // Act
        var result = await topaz.ListHierarchySettingsAsync(groupId);
        var values = result["value"]!.AsArray();

        // Assert
        Assert.That(values, Has.Count.EqualTo(1));
        Assert.That(values[0]!["name"]!.GetValue<string>(), Is.EqualTo("default"));
    }

    [Test]
    public async Task HierarchySettings_WhenNoSettingsExist_ListShouldReturnEmpty()
    {
        // Arrange
        const string groupId = "hs-list-empty-mg";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "HS List Empty MG");

        // Act
        var result = await topaz.ListHierarchySettingsAsync(groupId);
        var values = result["value"]!.AsArray();

        // Assert
        Assert.That(values, Is.Empty);
    }

    [Test]
    public async Task HierarchySettings_WhenUpdated_ShouldReturnUpdatedSettings()
    {
        // Arrange
        const string groupId = "hs-update-mg";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "HS Update MG");
        await topaz.CreateOrUpdateHierarchySettingsAsync(groupId,
            requireAuthorizationForGroupCreation: false);

        // Act
        var result = await topaz.UpdateHierarchySettingsAsync(groupId,
            requireAuthorizationForGroupCreation: true);

        // Assert
        Assert.That(result["properties"]!["requireAuthorizationForGroupCreation"]!.GetValue<bool>(), Is.True);
    }

    [Test]
    public async Task HierarchySettings_WhenDeleted_GetShouldReturn404()
    {
        // Arrange
        const string groupId = "hs-delete-mg";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "HS Delete MG");
        await topaz.CreateOrUpdateHierarchySettingsAsync(groupId);

        // Act
        await topaz.DeleteHierarchySettingsAsync(groupId);

        // Assert
        var ex = Assert.ThrowsAsync<HttpRequestException>(
            () => topaz.GetHierarchySettingsAsync(groupId));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }

    [Test]
    public async Task HierarchySettings_WhenManagementGroupNotFound_CreateShouldReturn404()
    {
        // Arrange
        var groupId = "hs-nonexistent-" + Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);

        // Act & Assert
        var ex = Assert.ThrowsAsync<HttpRequestException>(
            () => topaz.CreateOrUpdateHierarchySettingsAsync(groupId));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }

    [Test]
    public async Task HierarchySettings_WhenManagementGroupNotFound_GetShouldReturn404()
    {
        // Arrange
        var groupId = "hs-nonexistent-get-" + Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);

        // Act & Assert
        var ex = Assert.ThrowsAsync<HttpRequestException>(
            () => topaz.GetHierarchySettingsAsync(groupId));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }
}
