using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.Authorization.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class AuthorizationTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("C9FD7021-F94B-46C6-BC47-418C3EE67075");
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    
    [SetUp]
    public async Task SetUp()
    {
        await Program.Main(
        [
            "subscription",
            "delete",
            "--id",
            SubscriptionId.ToString()
        ]);
        
        await Program.Main(
        [
            "subscription",
            "create",
            "--id",
            SubscriptionId.ToString(),
            "--name",
            SubscriptionName
        ]);

        await Program.Main([
            "group",
            "delete",
            "--name",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.Main([
            "group",
            "create",
            "--name",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
    }

    [Test]
    public async Task RoleDefinition_CreateUpdateDelete_EmulatedCorrectly()
    {
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var roleDefinitions = subscription.GetAuthorizationRoleDefinitions();

        var roleDefinitionIdGuid = Guid.NewGuid();
        var roleDefinitionId = new ResourceIdentifier($"{roleDefinitionIdGuid}");
        var roleDefinitionData = new AuthorizationRoleDefinitionData
        {
            RoleName = "test-role-sdk",
            Description = "Test role",
        };
        roleDefinitionData.Permissions.Add(new RoleDefinitionPermission { Actions = { "Microsoft.Network/*/read" } });
        roleDefinitionData.AssignableScopes.Add($"/subscriptions/{SubscriptionId}");

        await roleDefinitions.CreateOrUpdateAsync(WaitUntil.Completed, roleDefinitionId, roleDefinitionData);

        var createdRoleDefinition = await roleDefinitions.GetAsync(roleDefinitionId);
        Assert.That(createdRoleDefinition.Value.Data.RoleName, Is.EqualTo("test-role-sdk"));

        createdRoleDefinition.Value.Data.Description = "Updated description";
        createdRoleDefinition.Value.Data.Permissions.Clear();
        createdRoleDefinition.Value.Data.Permissions.Add(new RoleDefinitionPermission { Actions = { "Microsoft.Network/*/read", "Microsoft.Network/*/write" } });

        await roleDefinitions.CreateOrUpdateAsync(WaitUntil.Completed, roleDefinitionId, createdRoleDefinition.Value.Data);

        var updatedRoleDefinition = await roleDefinitions.GetAsync(roleDefinitionId);
        Assert.That(updatedRoleDefinition.Value.Data.Description, Is.EqualTo("Updated description"));

        await createdRoleDefinition.Value.DeleteAsync(WaitUntil.Completed);

        Assert.ThrowsAsync<RequestFailedException>(async () => await roleDefinitions.GetAsync(roleDefinitionId));
    }

    [Test]
    [Ignore("TO BE IMPLEMENTED")]
    public async Task RoleAssignment_CreateAndDelete_EmulatedCorrectly()
    {
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var roleDefinitions = subscription.GetAuthorizationRoleDefinitions();
        
        var roleDefinitionIdGuid = Guid.NewGuid();
        var roleDefinitionId = new ResourceIdentifier($"/subscriptions/{SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{roleDefinitionIdGuid}");
        var roleDefinitionForAssign = new AuthorizationRoleDefinitionData { RoleName = "assignment-role", Description = "For assignment" };
        roleDefinitionForAssign.Permissions.Add(new RoleDefinitionPermission { Actions = { "*" } });
        roleDefinitionForAssign.AssignableScopes.Add($"/subscriptions/{SubscriptionId}");
        
        await roleDefinitions.CreateOrUpdateAsync(WaitUntil.Completed, roleDefinitionId, roleDefinitionForAssign);

        var roleAssignments = subscription.GetRoleAssignments();
        var roleAssignmentIdGuid = Guid.NewGuid();
        var roleAssignmentId = new ResourceIdentifier($"/subscriptions/{SubscriptionId}/providers/Microsoft.Authorization/roleAssignments/{roleAssignmentIdGuid}");
        var principalId = Guid.NewGuid();
        var assignmentData = new RoleAssignmentCreateOrUpdateContent(roleDefinitionId, principalId);

        await roleAssignments.CreateOrUpdateAsync(WaitUntil.Completed, roleAssignmentId, assignmentData);

        var createdAssignment = await roleAssignments.GetAsync(roleAssignmentId);
        Assert.That(createdAssignment.Value.Data.PrincipalId, Is.EqualTo(principalId));

        await createdAssignment.Value.DeleteAsync(WaitUntil.Completed, roleAssignmentId);
        Assert.ThrowsAsync<RequestFailedException>(async () => await roleAssignments.GetAsync(roleAssignmentId));
    }
}
