using Topaz.CLI;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.Authorization.Models;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
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
        await Program.RunAsync(
        [
            "subscription",
            "delete",
            "--id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "subscription",
            "create",
            "--id",
            SubscriptionId.ToString(),
            "--name",
            SubscriptionName
        ]);

        await Program.RunAsync([
            "group",
            "delete",
            "--name",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
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
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
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
        createdRoleDefinition.Value.Data.Permissions.Add(new RoleDefinitionPermission
            { Actions = { "Microsoft.Network/*/read", "Microsoft.Network/*/write" } });

        await roleDefinitions.CreateOrUpdateAsync(WaitUntil.Completed, roleDefinitionId,
            createdRoleDefinition.Value.Data);

        var updatedRoleDefinition = await roleDefinitions.GetAsync(roleDefinitionId);
        Assert.That(updatedRoleDefinition.Value.Data.Description, Is.EqualTo("Updated description"));

        await createdRoleDefinition.Value.DeleteAsync(WaitUntil.Completed);

        Assert.ThrowsAsync<RequestFailedException>(async () => await roleDefinitions.GetAsync(roleDefinitionId));
    }

    [Test]
    public async Task RoleAssignment_CreateAndDelete_EmulatedCorrectly()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var roleDefinitions = subscription.GetAuthorizationRoleDefinitions();

        var roleDefinitionIdGuid = Guid.NewGuid();
        var roleDefinitionId = new ResourceIdentifier($"{roleDefinitionIdGuid}");
        var roleDefinitionForAssign = new AuthorizationRoleDefinitionData
            { RoleName = "assignment-role", Description = "For assignment" };
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
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        var roleDefinitions = subscription.GetAuthorizationRoleDefinitions();

        var roleDefinitionIdGuid = Guid.NewGuid();
        var roleDefinitionId = new ResourceIdentifier($"{roleDefinitionIdGuid}");
        var roleDefinitionForAssign = new AuthorizationRoleDefinitionData
            { RoleName = "list-assignment-role", Description = "For assignment list" };
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
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        var roleDefinitions = subscription.GetAuthorizationRoleDefinitions();

        var roleDefinitionIdGuid = Guid.NewGuid();
        var roleDefinitionId = new ResourceIdentifier($"{roleDefinitionIdGuid}");
        var roleDefinitionForAssign = new AuthorizationRoleDefinitionData
            { RoleName = "scope-test-role", Description = "For scope test" };
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
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
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
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
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
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
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
                Assert.That(
                    results.Any(r => string.Equals(r.Data.RoleName, roleName, StringComparison.OrdinalIgnoreCase)),
                    Is.True, "Expected at least one result matching the filter");
                Assert.That(
                    results.All(r => string.Equals(r.Data.RoleName, roleName, StringComparison.OrdinalIgnoreCase)),
                    Is.True, "Found role definitions that do not match the filter");
            });
        }
        finally
        {
            var created = await roleDefinitions.GetAsync(roleDefinitionId);
            await created.Value.DeleteAsync(WaitUntil.Completed);
        }
    }

    [Test]
    public async Task RoleDefinition_List_CanFetchSinglePageSubset()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var roleDefinitions = subscription.GetAuthorizationRoleDefinitions();

        var createdRoleDefinitionIds = new List<ResourceIdentifier>();

        try
        {
            for (var i = 0; i < 3; i++)
            {
                var roleDefinitionIdGuid = Guid.NewGuid();
                var roleDefinitionId = new ResourceIdentifier($"{roleDefinitionIdGuid}");
                var roleDefinitionData = new AuthorizationRoleDefinitionData
                {
                    RoleName = $"paged-list-test-role-{roleDefinitionIdGuid:N}",
                    Description = $"Role used by paging test #{i}",
                };
                roleDefinitionData.Permissions.Add(new RoleDefinitionPermission
                    { Actions = { "Microsoft.Network/*/read" } });
                roleDefinitionData.AssignableScopes.Add($"/subscriptions/{SubscriptionId}");

                await roleDefinitions.CreateOrUpdateAsync(WaitUntil.Completed, roleDefinitionId, roleDefinitionData);
                createdRoleDefinitionIds.Add(roleDefinitionId);
            }

            Page<AuthorizationRoleDefinitionResource>? firstPage = null;
            await foreach (var page in roleDefinitions.GetAllAsync().AsPages(pageSizeHint: 2))
            {
                if(firstPage == null)
                {
                    firstPage = page;
                    continue;
                }
                
                // Just iterate over pages...
            }

            Assert.That(firstPage, Is.Not.Null, "Expected at least one page of role definitions");

            Assert.Multiple(() =>
            {
                Assert.That(firstPage!.Values, Has.Count.EqualTo(10),
                    "Expected to fetch only a subset of role definitions in the first page");
                Assert.That(firstPage.ContinuationToken, Is.Not.Null.And.Not.Empty,
                    "Expected a continuation token when more role definitions are available");
            });
        }
        finally
        {
            foreach (var roleDefinitionId in createdRoleDefinitionIds)
            {
                var created = await roleDefinitions.GetAsync(roleDefinitionId);
                await created.Value.DeleteAsync(WaitUntil.Completed);
            }
        }
    }

    [Test]
    public async Task RoleDefinition_GetById_ReturnsBuiltInRole()
    {
        // Reader is a well-known built-in role present in all environments
        const string readerRoleDefinitionId = "acdd72a7-3385-48ef-bd42-f606fba81ae7";
        var roleId = new ResourceIdentifier(
            $"/providers/Microsoft.Authorization/roleDefinitions/{readerRoleDefinitionId}");

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);

        var roleDefResource = armClient.GetAuthorizationRoleDefinitionResource(roleId);
        var result = await roleDefResource.GetAsync();

        Assert.That(result.Value, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Data.RoleName, Is.EqualTo("Reader"));
            Assert.That(result.Value.Data.Id.ToString(),
                Does.Contain(readerRoleDefinitionId));
        });
    }

    // -------------------------------------------------------------------------
    // Hierarchy propagation tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// A role assignment at subscription scope propagates down to resources inside that subscription.
    /// </summary>
    [Test]
    public async Task RoleAssignment_SubscriptionScope_PermissionPropagatesDownToResource()
    {
        var principalId = Guid.NewGuid();
        var adminCredential = new AzureLocalCredential(Globals.GlobalAdminId);
        var adminClient = new ArmClient(adminCredential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await adminClient.GetDefaultSubscriptionAsync();
        var rg = (await subscription.GetResourceGroupAsync(ResourceGroupName)).Value;

        // Create a Key Vault
        var vaultName = $"hiersub{Guid.NewGuid():N}"[..20];
        rg.GetKeyVaults().CreateOrUpdate(WaitUntil.Completed, vaultName,
            new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
                new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))));

        // Create a role definition that grants Key Vault read
        var roleDefId = new ResourceIdentifier($"{Guid.NewGuid()}");
        var roleDefData = new AuthorizationRoleDefinitionData
            { RoleName = $"hier-sub-{roleDefId}", Description = "Grants KV read" };
        roleDefData.Permissions.Add(new RoleDefinitionPermission { Actions = { "Microsoft.KeyVault/vaults/read" } });
        roleDefData.AssignableScopes.Add($"/subscriptions/{SubscriptionId}");
        await subscription.GetAuthorizationRoleDefinitions()
            .CreateOrUpdateAsync(WaitUntil.Completed, roleDefId, roleDefData);

        // Assign at SUBSCRIPTION scope
        var assignmentId = new ResourceIdentifier($"{Guid.NewGuid()}");
        await subscription.GetRoleAssignments()
            .CreateOrUpdateAsync(WaitUntil.Completed, assignmentId,
                new RoleAssignmentCreateOrUpdateContent(roleDefId, principalId));

        // Non-admin reads the vault directly by resource ID — requires only KV read permission
        var nonAdminClient = new ArmClient(new AzureLocalCredential(principalId.ToString()),
            SubscriptionId.ToString(), ArmClientOptions);
        var vaultId = new ResourceIdentifier(
            $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.KeyVault/vaults/{vaultName}");

        Assert.DoesNotThrowAsync(
            async () => await nonAdminClient.GetKeyVaultResource(vaultId).GetAsync(),
            "Principal with subscription-scope role assignment should be able to read Key Vault.");
    }

    /// <summary>
    /// A role assignment at resource-group scope propagates to resources in that RG
    /// but does NOT grant access to resources in a sibling resource group.
    /// </summary>
    [Test]
    public async Task RoleAssignment_ResourceGroupScope_PermissionPropagatesDownButNotToSiblingRg()
    {
        var principalId = Guid.NewGuid();
        var rgA = "hier-rg-a";
        var rgB = "hier-rg-b";

        // Set up two resource groups
        await Program.RunAsync(["group", "delete", "--name", rgA, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "delete", "--name", rgB, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", rgA, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", rgB, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        var adminCredential = new AzureLocalCredential(Globals.GlobalAdminId);
        var adminClient = new ArmClient(adminCredential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await adminClient.GetDefaultSubscriptionAsync();

        // Create one Key Vault in each RG
        var vaultA = $"hierrga{Guid.NewGuid():N}"[..20];
        var vaultB = $"hierrgb{Guid.NewGuid():N}"[..20];
        (await subscription.GetResourceGroupAsync(rgA)).Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, vaultA,
                new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
                    new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))));
        (await subscription.GetResourceGroupAsync(rgB)).Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, vaultB,
                new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
                    new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))));

        // Create a role definition granting Key Vault read
        var roleDefId = new ResourceIdentifier($"{Guid.NewGuid()}");
        var roleDefData = new AuthorizationRoleDefinitionData
            { RoleName = $"hier-rg-{roleDefId}", Description = "Grants KV read at RG-A" };
        roleDefData.Permissions.Add(new RoleDefinitionPermission { Actions = { "Microsoft.KeyVault/vaults/read" } });
        roleDefData.AssignableScopes.Add($"/subscriptions/{SubscriptionId}");
        await subscription.GetAuthorizationRoleDefinitions()
            .CreateOrUpdateAsync(WaitUntil.Completed, roleDefId, roleDefData);

        // Assign at RG-A scope using the new RG-scoped endpoint
        using var topaz = new TopazArmClient(adminCredential);
        await topaz.CreateResourceGroupRoleAssignmentAsync(
            SubscriptionId, rgA, Guid.NewGuid().ToString(),
            principalId.ToString(), roleDefId.ToString());

        var nonAdminClient = new ArmClient(new AzureLocalCredential(principalId.ToString()),
            SubscriptionId.ToString(), ArmClientOptions);

        var vaultAId = new ResourceIdentifier(
            $"/subscriptions/{SubscriptionId}/resourceGroups/{rgA}/providers/Microsoft.KeyVault/vaults/{vaultA}");
        var vaultBId = new ResourceIdentifier(
            $"/subscriptions/{SubscriptionId}/resourceGroups/{rgB}/providers/Microsoft.KeyVault/vaults/{vaultB}");

        // Access to vault in RG-A should succeed (within the assigned scope)
        Assert.DoesNotThrowAsync(
            async () => await nonAdminClient.GetKeyVaultResource(vaultAId).GetAsync(),
            "Principal with RG-A-scoped role assignment should read vault in RG-A.");

        // Access to vault in RG-B should be denied (outside the assigned scope)
        Assert.ThrowsAsync<RequestFailedException>(
            async () => await nonAdminClient.GetKeyVaultResource(vaultBId).GetAsync(),
            "Principal with RG-A-scoped role assignment should NOT read vault in RG-B.");
    }

    /// <summary>
    /// A role assignment at management-group scope propagates down through
    /// the hierarchy to resources inside subscriptions under that MG.
    /// </summary>
    [Test]
    public async Task RoleAssignment_ManagementGroupScope_PermissionPropagatesDownThroughHierarchy()
    {
        var principalId = Guid.NewGuid();
        var mgId = $"hiermg{Guid.NewGuid():N}"[..20];

        var adminCredential = new AzureLocalCredential(Globals.GlobalAdminId);
        var adminClient = new ArmClient(adminCredential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await adminClient.GetDefaultSubscriptionAsync();
        var rg = (await subscription.GetResourceGroupAsync(ResourceGroupName)).Value;

        // Create a Key Vault
        var vaultName = $"hiermg{Guid.NewGuid():N}"[..20];
        rg.GetKeyVaults().CreateOrUpdate(WaitUntil.Completed, vaultName,
            new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
                new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))));

        // Create MG and associate the subscription under it
        using var topaz = new TopazArmClient(adminCredential);
        await topaz.CreateManagementGroupAsync(mgId, "Hierarchy Test MG");
        await topaz.AssociateSubscriptionWithManagementGroupAsync(mgId, SubscriptionId.ToString());

        // Create a role definition granting Key Vault read
        var roleDefId = new ResourceIdentifier($"{Guid.NewGuid()}");
        var roleDefData = new AuthorizationRoleDefinitionData
            { RoleName = $"hier-mg-{roleDefId}", Description = "Grants KV read at MG scope" };
        roleDefData.Permissions.Add(new RoleDefinitionPermission { Actions = { "Microsoft.KeyVault/vaults/read" } });
        roleDefData.AssignableScopes.Add($"/subscriptions/{SubscriptionId}");
        await subscription.GetAuthorizationRoleDefinitions()
            .CreateOrUpdateAsync(WaitUntil.Completed, roleDefId, roleDefData);

        // Assign at MANAGEMENT GROUP scope
        await topaz.CreateManagementGroupRoleAssignmentAsync(
            mgId, Guid.NewGuid().ToString(),
            principalId.ToString(), roleDefId.ToString());

        // Non-admin reads vault directly by resource ID — should succeed via MG-level assignment
        var nonAdminClient = new ArmClient(new AzureLocalCredential(principalId.ToString()),
            SubscriptionId.ToString(), ArmClientOptions);
        var vaultId = new ResourceIdentifier(
            $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.KeyVault/vaults/{vaultName}");

        Assert.DoesNotThrowAsync(
            async () => await nonAdminClient.GetKeyVaultResource(vaultId).GetAsync(),
            "Principal with MG-scope role assignment should be able to read Key Vault in a child subscription.");
    }

    /// <summary>
    /// A role assignment scoped to an exact resource does NOT grant access to sibling resources
    /// at the same scope level.
    /// </summary>
    [Test]
    public async Task RoleAssignment_ResourceScopeOnly_DoesNotPropagateToSiblingResource()
    {
        var principalId = Guid.NewGuid();
        var adminCredential = new AzureLocalCredential(Globals.GlobalAdminId);
        var adminClient = new ArmClient(adminCredential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await adminClient.GetDefaultSubscriptionAsync();
        var rg = (await subscription.GetResourceGroupAsync(ResourceGroupName)).Value;

        // Create two Key Vaults in the same RG
        var vaultA = $"hierresa{Guid.NewGuid():N}"[..20];
        var vaultB = $"hierresb{Guid.NewGuid():N}"[..20];
        rg.GetKeyVaults().CreateOrUpdate(WaitUntil.Completed, vaultA,
            new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
                new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))));
        rg.GetKeyVaults().CreateOrUpdate(WaitUntil.Completed, vaultB,
            new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
                new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))));

        // Create a role definition granting Key Vault read
        var roleDefId = new ResourceIdentifier($"{Guid.NewGuid()}");
        var roleDefData = new AuthorizationRoleDefinitionData
            { RoleName = $"hier-res-{roleDefId}", Description = "Grants KV read at resource scope" };
        roleDefData.Permissions.Add(new RoleDefinitionPermission { Actions = { "Microsoft.KeyVault/vaults/read" } });
        roleDefData.AssignableScopes.Add($"/subscriptions/{SubscriptionId}");
        await subscription.GetAuthorizationRoleDefinitions()
            .CreateOrUpdateAsync(WaitUntil.Completed, roleDefId, roleDefData);

        // Assign at the exact resource scope of vaultA
        using var topaz = new TopazArmClient(adminCredential);
        await topaz.CreateResourceRoleAssignmentAsync(
            SubscriptionId, ResourceGroupName, "Microsoft.KeyVault", "vaults", vaultA,
            Guid.NewGuid().ToString(), principalId.ToString(), roleDefId.ToString());

        var nonAdminClient = new ArmClient(new AzureLocalCredential(principalId.ToString()),
            SubscriptionId.ToString(), ArmClientOptions);

        var vaultAId = new ResourceIdentifier(
            $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.KeyVault/vaults/{vaultA}");
        var vaultBId = new ResourceIdentifier(
            $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.KeyVault/vaults/{vaultB}");

        // vaultA — within the assigned scope → should succeed
        Assert.DoesNotThrowAsync(
            async () => await nonAdminClient.GetKeyVaultResource(vaultAId).GetAsync(),
            "Principal with resource-scoped assignment on vaultA should be able to read vaultA.");

        // vaultB — sibling, outside the assigned scope → should be denied
        Assert.ThrowsAsync<RequestFailedException>(
            async () => await nonAdminClient.GetKeyVaultResource(vaultBId).GetAsync(),
            "Principal with resource-scoped assignment on vaultA should NOT be able to read vaultB.");
    }
}