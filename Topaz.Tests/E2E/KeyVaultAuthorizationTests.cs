using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.Authorization.Models;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.Security.KeyVault.Secrets;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

/// <summary>
/// Tests that Key Vault data-plane operations respect the vault's authorization mode:
/// - Access Policy mode (<c>enableRbacAuthorization = false</c>): only principals listed in the
///   vault's accessPolicies with the required permission may perform the operation.
/// - RBAC mode (<c>enableRbacAuthorization = true</c>): only principals holding a role assignment
///   that grants the required ARM action may perform the operation.
/// </summary>
public class KeyVaultAuthorizationTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("B5E21A03-8D4F-4C90-A7BF-000000000001");

    private const string SubscriptionName = "sub-kv-auth-test";
    private const string ResourceGroupName = "rg-kv-auth-test";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
    }

    // -------------------------------------------------------------------------
    // Access Policy tests
    // -------------------------------------------------------------------------

    [Test]
    public void KeyVaultAuthorization_AccessPolicy_AllowedPrincipalCanSetSecret()
    {
        var principalId = Guid.NewGuid();

        // Create vault with access policies (RBAC disabled).
        var vault = CreateVault("authpolicyallow", enableRbac: false);

        // Grant the principal "set" on secrets.
        vault.UpdateAccessPolicy(AccessPolicyUpdateKind.Add,
            new KeyVaultAccessPolicyParameters(new KeyVaultAccessPolicyProperties(
            [
                new KeyVaultAccessPolicy(Guid.Empty, principalId.ToString(),
                    new IdentityAccessPermissions { Secrets = { IdentityAccessSecretPermission.Set } })
            ])));

        // Call SetSecret as that principal.
        var secretClient = BuildSecretClient("authpolicyallow", principalId);
        var result = secretClient.SetSecret("test-secret", "hello-world");

        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.Value, Is.EqualTo("hello-world"));
    }

    [Test]
    public void KeyVaultAuthorization_AccessPolicy_UnauthorizedPrincipalIsRejected()
    {
        var principalId = Guid.NewGuid();

        // Create vault with access policies — but do NOT add a policy for our principal.
        CreateVault("authpolicydeny", enableRbac: false);

        var secretClient = BuildSecretClient("authpolicydeny", principalId);

        Assert.Throws<RequestFailedException>(() => secretClient.SetSecret("test-secret", "should-fail"));
    }

    // -------------------------------------------------------------------------
    // RBAC tests
    // -------------------------------------------------------------------------

    [Test]
    public async Task KeyVaultAuthorization_Rbac_PrincipalWithRoleCanSetSecret()
    {
        var principalId = Guid.NewGuid();

        // Create vault with RBAC enabled.
        CreateVault("authrbacallow", enableRbac: true);

        // Create a role definition that allows setSecret.
        var adminCredential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(adminCredential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        var roleDefId = new ResourceIdentifier($"{Guid.NewGuid()}");
        var roleDefData = new AuthorizationRoleDefinitionData { RoleName = "kv-secret-writer", Description = "Allows setting secrets" };
        roleDefData.Permissions.Add(new RoleDefinitionPermission
        {
            Actions = { "Microsoft.KeyVault/vaults/secrets/setSecret/action" }
        });
        roleDefData.AssignableScopes.Add($"/subscriptions/{SubscriptionId}");
        await subscription.GetAuthorizationRoleDefinitions()
            .CreateOrUpdateAsync(WaitUntil.Completed, roleDefId, roleDefData);

        // Assign it to the test principal.
        var roleAssignmentId = new ResourceIdentifier($"{Guid.NewGuid()}");
        await subscription.GetRoleAssignments()
            .CreateOrUpdateAsync(WaitUntil.Completed, roleAssignmentId,
                new RoleAssignmentCreateOrUpdateContent(roleDefId, principalId));

        // Call SetSecret as that principal.
        var secretClient = BuildSecretClient("authrbacallow", principalId);
        var result = secretClient.SetSecret("rbac-secret", "rbac-value");

        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.Value, Is.EqualTo("rbac-value"));
    }

    [Test]
    public void KeyVaultAuthorization_Rbac_PrincipalWithoutRoleIsRejected()
    {
        var principalId = Guid.NewGuid();

        // Create vault with RBAC enabled — no role assignment for the principal.
        CreateVault("authrbacDeny", enableRbac: true);

        var secretClient = BuildSecretClient("authrbacDeny", principalId);

        Assert.Throws<RequestFailedException>(() => secretClient.SetSecret("rbac-secret", "should-fail"));
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private KeyVaultResource CreateVault(string name, bool enableRbac)
    {
        var adminCredential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(adminCredential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName);

        var content = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))
            {
                EnableRbacAuthorization = enableRbac
            });

        resourceGroup.Value.GetKeyVaults().CreateOrUpdate(WaitUntil.Completed, name, content);
        return resourceGroup.Value.GetKeyVault(name).Value;
    }

    private SecretClient BuildSecretClient(string vaultName, Guid principalId)
    {
        return new SecretClient(
            TopazResourceHelpers.GetKeyVaultEndpoint(vaultName),
            new AzureLocalCredential(principalId.ToString()),
            new SecretClientOptions { DisableChallengeResourceVerification = true });
    }
}
