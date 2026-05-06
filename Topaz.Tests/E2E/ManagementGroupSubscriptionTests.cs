using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ManagementGroupSubscriptionTests
{
    [Test]
    public async Task ManagementGroupSubscription_WhenSubscriptionIsAssociated_ShouldReturnAssociation()
    {
        // Arrange
        const string groupId = "mg-sub-assoc-test";
        var subscriptionId = Guid.NewGuid().ToString();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "MG Sub Association Test");

        // Act
        var result = await topaz.AssociateSubscriptionWithManagementGroupAsync(groupId, subscriptionId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result["name"]!.GetValue<string>(), Is.EqualTo(subscriptionId));
            Assert.That(result["type"]!.GetValue<string>(),
                Is.EqualTo("Microsoft.Management/managementGroups/subscriptions"));
            Assert.That(result["id"]!.GetValue<string>(),
                Is.EqualTo($"/providers/Microsoft.Management/managementGroups/{groupId}/subscriptions/{subscriptionId}"));
            Assert.That(result["properties"]!["state"]!.GetValue<string>(), Is.EqualTo("Active"));
        });
    }

    [Test]
    public async Task ManagementGroupSubscription_WhenSubscriptionIsAssociated_GetShouldReturnIt()
    {
        // Arrange
        const string groupId = "mg-sub-get-test";
        var subscriptionId = Guid.NewGuid().ToString();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "MG Sub Get Test");
        await topaz.AssociateSubscriptionWithManagementGroupAsync(groupId, subscriptionId);

        // Act
        var result = await topaz.GetSubscriptionUnderManagementGroupAsync(groupId, subscriptionId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result["name"]!.GetValue<string>(), Is.EqualTo(subscriptionId));
            Assert.That(result["type"]!.GetValue<string>(),
                Is.EqualTo("Microsoft.Management/managementGroups/subscriptions"));
            Assert.That(result["properties"]!["parent"]!["name"]!.GetValue<string>(), Is.EqualTo(groupId));
        });
    }

    [Test]
    public async Task ManagementGroupSubscription_WhenSubscriptionIsDisassociated_GetShouldReturn404()
    {
        // Arrange
        const string groupId = "mg-sub-delete-test";
        var subscriptionId = Guid.NewGuid().ToString();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "MG Sub Delete Test");
        await topaz.AssociateSubscriptionWithManagementGroupAsync(groupId, subscriptionId);

        // Act
        await topaz.DisassociateSubscriptionFromManagementGroupAsync(groupId, subscriptionId);

        // Assert
        var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await topaz.GetSubscriptionUnderManagementGroupAsync(groupId, subscriptionId));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ManagementGroupSubscription_WhenManagementGroupDoesNotExist_AssociateShouldReturn404()
    {
        // Arrange
        const string groupId = "mg-sub-nonexistent-mg";
        var subscriptionId = Guid.NewGuid().ToString();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);

        // Act & Assert
        var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await topaz.AssociateSubscriptionWithManagementGroupAsync(groupId, subscriptionId));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ManagementGroupSubscription_WhenSubscriptionNotAssociated_GetShouldReturn404()
    {
        // Arrange
        const string groupId = "mg-sub-get-missing-test";
        var subscriptionId = Guid.NewGuid().ToString();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "MG Sub Missing Test");

        // Act & Assert
        var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await topaz.GetSubscriptionUnderManagementGroupAsync(groupId, subscriptionId));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }
}
