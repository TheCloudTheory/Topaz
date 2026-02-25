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
    public async Task RoleAssignment_CreateAndDelete_EmulatedCorrectly()
    {
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var roleDefinitions = subscription.GetAuthorizationRoleDefinitions();
        
        var roleDefinitionIdGuid = Guid.NewGuid();
        var roleDefinitionId = new ResourceIdentifier($"{roleDefinitionIdGuid}");
        var roleDefinitionForAssign = new AuthorizationRoleDefinitionData { RoleName = "assignment-role", Description = "For assignment" };
        roleDefinitionForAssign.Permissions.Add(new RoleDefinitionPermission { Actions = { "*" } });
        roleDefinitionForAssign.AssignableScopes.Add($"/subscriptions/{SubscriptionId}");
        
        await roleDefinitions.CreateOrUpdateAsync(WaitUntil.Completed, roleDefinitionId, roleDefinitionForAssign);

        var roleAssignments = subscription.GetRoleAssignments();
        var roleAssignmentIdGuid = Guid.NewGuid();
        var roleAssignmentId = new ResourceIdentifier($"{roleAssignmentIdGuid}");
        var principalId = Guid.NewGuid();
        var assignmentData = new RoleAssignmentCreateOrUpdateContent(roleDefinitionId, principalId);

        await roleAssignments.CreateOrUpdateAsync(WaitUntil.Completed, roleAssignmentId, assignmentData);

        var createdAssignment = await roleAssignments.GetAsync(roleAssignmentId);
        Assert.That(createdAssignment.Value.Data.PrincipalId, Is.EqualTo(principalId));

        await createdAssignment.Value.DeleteAsync(WaitUntil.Completed, roleAssignmentId);
        Assert.ThrowsAsync<RequestFailedException>(async () => await roleAssignments.GetAsync(roleAssignmentId));
    }

    [Test]
    public async Task RoleAssignment_List_IsEnumerableAndContainsCreatedAssignment()
    {
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        var roleDefinitions = subscription.GetAuthorizationRoleDefinitions();

        var roleDefinitionIdGuid = Guid.NewGuid();
        var roleDefinitionId = new ResourceIdentifier($"{roleDefinitionIdGuid}");
        var roleDefinitionForAssign = new AuthorizationRoleDefinitionData { RoleName = "list-assignment-role", Description = "For assignment list" };
        roleDefinitionForAssign.Permissions.Add(new RoleDefinitionPermission { Actions = { "*" } });
        roleDefinitionForAssign.AssignableScopes.Add($"/subscriptions/{SubscriptionId}");

        await roleDefinitions.CreateOrUpdateAsync(WaitUntil.Completed, roleDefinitionId, roleDefinitionForAssign);

        var roleAssignments = subscription.GetRoleAssignments();
        var roleAssignmentIdGuid = Guid.NewGuid();
        var roleAssignmentId = new ResourceIdentifier($"{roleAssignmentIdGuid}");
        var principalId = Guid.NewGuid();
        var assignmentData = new RoleAssignmentCreateOrUpdateContent(roleDefinitionId, principalId);

        await roleAssignments.CreateOrUpdateAsync(WaitUntil.Completed, roleAssignmentId, assignmentData);

        try
        {
            var found = false;
            await foreach (var ra in roleAssignments.GetAllAsync())
            {
                if (ra.Data.PrincipalId != principalId) continue;
                found = true;
                break;
            }

            Assert.That(found, Is.True);
        }
        finally
        {
            var createdAssignment = await roleAssignments.GetAsync(roleAssignmentId);
            await createdAssignment.Value.DeleteAsync(WaitUntil.Completed, roleAssignmentId);

            var createdRole = await roleDefinitions.GetAsync(roleDefinitionId);
            await createdRole.Value.DeleteAsync(WaitUntil.Completed);
        }
    }

    [Test]
    public async Task RoleAssignment_CreateWithSubscriptionScope_SetsScopeToSubscription()
    {
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        var roleDefinitions = subscription.GetAuthorizationRoleDefinitions();

        var roleDefinitionIdGuid = Guid.NewGuid();
        var roleDefinitionId = new ResourceIdentifier($"{roleDefinitionIdGuid}");
        var roleDefinitionForAssign = new AuthorizationRoleDefinitionData { RoleName = "scope-test-role", Description = "For scope test" };
        roleDefinitionForAssign.Permissions.Add(new RoleDefinitionPermission { Actions = { "*" } });
        roleDefinitionForAssign.AssignableScopes.Add($"/subscriptions/{SubscriptionId}");

        await roleDefinitions.CreateOrUpdateAsync(WaitUntil.Completed, roleDefinitionId, roleDefinitionForAssign);

        var roleAssignments = subscription.GetRoleAssignments();
        var roleAssignmentIdGuid = Guid.NewGuid();
        var roleAssignmentId = new ResourceIdentifier($"{roleAssignmentIdGuid}");
        var principalId = Guid.NewGuid();
        var assignmentData = new RoleAssignmentCreateOrUpdateContent(roleDefinitionId, principalId);

        await roleAssignments.CreateOrUpdateAsync(WaitUntil.Completed, roleAssignmentId, assignmentData);

        try
        {
            var created = await roleAssignments.GetAsync(roleAssignmentId);
            Assert.That(created.Value.Data.Scope, Is.EqualTo($"/subscriptions/{SubscriptionId}"));
        }
        finally
        {
            var createdAssignment = await roleAssignments.GetAsync(roleAssignmentId);
            await createdAssignment.Value.DeleteAsync(WaitUntil.Completed, roleAssignmentId);

            var createdRole = await roleDefinitions.GetAsync(roleDefinitionId);
            await createdRole.Value.DeleteAsync(WaitUntil.Completed);
        }
    }

    [Test]
    public async Task RoleDefinition_List_IsEnumerableAndWellFormed()
    {
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var roleDefinitions = subscription.GetAuthorizationRoleDefinitions();

        // create a role definition to ensure there is at least one to enumerate
        var roleDefinitionIdGuid = Guid.NewGuid();
        var roleDefinitionId = new ResourceIdentifier($"{roleDefinitionIdGuid}");
        var roleDefinitionData = new AuthorizationRoleDefinitionData
        {
            RoleName = "list-test-role",
            Description = "Role used by list test",
        };
        roleDefinitionData.Permissions.Add(new RoleDefinitionPermission { Actions = { "Microsoft.Network/*/read" } });
        roleDefinitionData.AssignableScopes.Add($"/subscriptions/{SubscriptionId}");

        await roleDefinitions.CreateOrUpdateAsync(WaitUntil.Completed, roleDefinitionId, roleDefinitionData);

        var created = await roleDefinitions.GetAsync(roleDefinitionId);

        var foundAny = false;
        await foreach (var rd in roleDefinitions.GetAllAsync())
        {
            foundAny = true;
            Assert.That(rd.Data, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(rd.Data.RoleName, Is.Not.Null.And.Not.Empty);
                Assert.That(rd.Data.AssignableScopes, Is.Not.Null);
            });
        }

        Assert.That(foundAny, Is.True);

        // cleanup
        await created.Value.DeleteAsync(WaitUntil.Completed);
    }

    [Test]
    public async Task RoleDefinition_List_IncludesBuiltInRole()
    {
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var roleDefinitions = subscription.GetAuthorizationRoleDefinitions();

        var containsBuiltIn = false;
        await foreach (var rd in roleDefinitions.GetAllAsync())
        {
            if (!string.Equals(rd.Data.RoleName, "Reader", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(rd.Data.RoleName, "Contributor", StringComparison.OrdinalIgnoreCase)) continue;
            
            containsBuiltIn = true;
            break;
        }

        Assert.That(containsBuiltIn, Is.True);
    }

    [Test]
    public async Task RoleDefinition_List_FilterFindsSpecificRole()
    {
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var roleDefinitions = subscription.GetAuthorizationRoleDefinitions();

        var roleDefinitionIdGuid = Guid.NewGuid();
        var roleDefinitionId = new ResourceIdentifier($"{roleDefinitionIdGuid}");
        var roleName = $"filter-test-role-{roleDefinitionIdGuid:N}";
        var roleDefinitionData = new AuthorizationRoleDefinitionData
        {
            RoleName = roleName,
            Description = "Role used by filter test",
        };
        roleDefinitionData.Permissions.Add(new RoleDefinitionPermission { Actions = { "Microsoft.Network/*/read" } });
        roleDefinitionData.AssignableScopes.Add($"/subscriptions/{SubscriptionId}");

        await roleDefinitions.CreateOrUpdateAsync(WaitUntil.Completed, roleDefinitionId, roleDefinitionData);

        try
        {
            var results = new List<AuthorizationRoleDefinitionResource>();
            var filter = $"roleName eq '{roleName}'";
            await foreach (var rd in roleDefinitions.GetAllAsync(filter))
            {
                results.Add(rd);
            }

            Assert.Multiple(() =>
            {
                Assert.That(results.Any(r => string.Equals(r.Data.RoleName, roleName, StringComparison.OrdinalIgnoreCase)), Is.True, "Expected at least one result matching the filter");
                Assert.That(results.All(r => string.Equals(r.Data.RoleName, roleName, StringComparison.OrdinalIgnoreCase)), Is.True, "Found role definitions that do not match the filter");
            });
        }
        finally
        {
            var created = await roleDefinitions.GetAsync(roleDefinitionId);
            await created.Value.DeleteAsync(WaitUntil.Completed);
        }
    }
}
