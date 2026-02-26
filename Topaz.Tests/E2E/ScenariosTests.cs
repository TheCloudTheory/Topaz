using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.ManagedServiceIdentities;
using Azure.Security.KeyVault.Secrets;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ScenariosTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    private static readonly Guid SubscriptionId = Guid.Parse("5B6A53C7-2A55-4C4C-9E68-0123456789AB");
    private const string SubscriptionName = "sub-test-scenarios";
    private const string ResourceGroupName = "rg-test-scenarios";
    private const string ReaderRoleDefinitionId =
        "acdd72a7-3385-48ef-bd42-f606fba81ae7";
    private const string KeyVaultContributorRoleDefinitionId =
        "f25e0fa2-a7c8-4377-a976-54943a77a395";
    private const string KeyVaultSecretsUserRoleDefinitionId =
        "4633458b-17de-408a-b874-0445c86b69e6";

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
    public async Task ScenariosTests_WhenKeyVaultAndManagedIdentityAreCreatedUsingSDK_AndIdentityHasNoSubscriptionRole_ThenIdentityCannotDoControlPlaneOpsOnKeyVault()
    {
        // Arrange (create resources as an "admin" caller)
        var adminCredential = new AzureLocalCredential(Globals.GlobalAdminId);
        var adminArmClient = new ArmClient(adminCredential, SubscriptionId.ToString(), ArmClientOptions);

        var subscription = await adminArmClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var unique = Guid.NewGuid().ToString("N")[..12];
        var identityName = $"mi-norole-{unique}";
        var kvName = $"kvnorole{unique}";

        var identityCollection = resourceGroup.Value.GetUserAssignedIdentities();
        await identityCollection.CreateOrUpdateAsync(
            WaitUntil.Completed,
            identityName,
            new UserAssignedIdentityData(AzureLocation.WestEurope),
            CancellationToken.None);

        var identity = await resourceGroup.Value.GetUserAssignedIdentityAsync(identityName);
        Assert.That(identity.Value.Data.PrincipalId, Is.Not.Null, "Managed Identity PrincipalId should be set.");

        var kvCollection = resourceGroup.Value.GetKeyVaults();
        var kvCreate = new KeyVaultCreateOrUpdateContent(
            AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));

        await kvCollection.CreateOrUpdateAsync(WaitUntil.Completed, kvName, kvCreate, CancellationToken.None);

        // Act (attempt control-plane operations as the managed identity WITHOUT any role assignment)
        var managedIdentityCredential = new ManagedIdentityLocalCredential(identity.Value.Data.PrincipalId!.Value);
        var miArmClient = new ArmClient(managedIdentityCredential, SubscriptionId.ToString(), ArmClientOptions);

        // Assert (a MI with no roles can't even read the subscription)
        var subEx = Assert.ThrowsAsync<RequestFailedException>(async () =>
            await miArmClient.GetDefaultSubscriptionAsync(CancellationToken.None));

        Assert.That(subEx, Is.Not.Null);
        Assert.That(subEx!.Status, Is.EqualTo(401),
            $"Expected 401 Not Authorized when reading subscription, got {subEx.Status}. ErrorCode: {subEx.ErrorCode}");

        // Optional: also assert Key Vault GET is forbidden when addressed directly (without going via subscription/RG listing)
        var kvId =
            $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.KeyVault/vaults/{kvName}";
        var kvResourceIdentifier = new ResourceIdentifier(kvId);

        var kvEx = Assert.ThrowsAsync<RequestFailedException>(async () =>
            await miArmClient.GetKeyVaultResource(kvResourceIdentifier).GetAsync(CancellationToken.None));

        Assert.That(kvEx, Is.Not.Null);
        Assert.That(kvEx!.Status, Is.EqualTo(401),
            $"Expected 401 Not Authorized when reading Key Vault, got {kvEx.Status}. ErrorCode: {kvEx.ErrorCode}");
    }
    
    [Test]
    public async Task ScenariosTests_WhenKeyVaultAndManagedIdentityAreCreated_AndIdentityHasCorrectRoles_ThenIdentityCanDoControlPlaneAndDataPlaneOpsOnKeyVault()
    {
        // Arrange (create resources as an "admin" caller)
        var adminCredential = new AzureLocalCredential(Globals.GlobalAdminId);
        var adminArmClient = new ArmClient(adminCredential, SubscriptionId.ToString(), ArmClientOptions);

        var subscription = await adminArmClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var unique = Guid.NewGuid().ToString("N")[..12];
        var identityName = $"mi-kvfull-{unique}";
        var kvName = $"kvfull{unique}";

        var identityCollection = resourceGroup.Value.GetUserAssignedIdentities();
        await identityCollection.CreateOrUpdateAsync(
            WaitUntil.Completed,
            identityName,
            new UserAssignedIdentityData(AzureLocation.WestEurope),
            CancellationToken.None);

        var identity = await resourceGroup.Value.GetUserAssignedIdentityAsync(identityName);
        Assert.That(identity.Value.Data.PrincipalId, Is.Not.Null, "Managed Identity PrincipalId should be set.");
        var principalId = identity.Value.Data.PrincipalId!.Value;

        var kvCollection = resourceGroup.Value.GetKeyVaults();
        var kvCreate = new KeyVaultCreateOrUpdateContent(
            AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));

        await kvCollection.CreateOrUpdateAsync(WaitUntil.Completed, kvName, kvCreate, CancellationToken.None);

        var subscriptionScope = $"/subscriptions/{SubscriptionId}";
        var resourceGroupScope = $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}";
        var keyVaultScope = $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.KeyVault/vaults/{kvName}";

        // Grant subscription read so the MI can do control-plane discovery.
        await CreateRoleAssignmentAsync(ReaderRoleDefinitionId, principalId, subscriptionScope);

        // Grant Key Vault management-plane access (control plane ops on the vault resource).
        await CreateRoleAssignmentAsync(KeyVaultContributorRoleDefinitionId, principalId, resourceGroupScope);

        // Grant Key Vault secrets data-plane access.
        await CreateRoleAssignmentAsync(KeyVaultSecretsUserRoleDefinitionId, principalId, keyVaultScope);

        // Act (perform control-plane + data-plane operations as the managed identity)
        var managedIdentityCredential = new ManagedIdentityLocalCredential(principalId);
        var miArmClient = new ArmClient(managedIdentityCredential, SubscriptionId.ToString(), ArmClientOptions);

        var miSubscription = await miArmClient.GetDefaultSubscriptionAsync();
        var miResourceGroup = await miSubscription.GetResourceGroupAsync(ResourceGroupName);

        var kv = await miResourceGroup.Value.GetKeyVaultAsync(kvName, CancellationToken.None);
        Assert.That(kv.Value, Is.Not.Null);

        var secretClient = new SecretClient(
            vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(kvName),
            credential: managedIdentityCredential,
            options: new SecretClientOptions
            {
                DisableChallengeResourceVerification = true
            });

        var secretName = $"s-{unique}";
        _ = await secretClient.SetSecretAsync(secretName, "test", CancellationToken.None);
        var secret = await secretClient.GetSecretAsync(secretName, cancellationToken: CancellationToken.None);

        // Assert
        Assert.That(secret.Value, Is.Not.Null);
        Assert.That(secret.Value.Value, Is.EqualTo("test"));
    }
    
    private static Task CreateRoleAssignmentAsync(string roleDefinitionId, Guid principalId, string scope)
    {
        // NOTE: Adjust command/flags to whatever your Topaz CLI exposes for role assignments.
        // This is intended to call:
        // PUT {scope}/providers/Microsoft.Authorization/roleAssignments/{roleAssignmentName}
        var roleAssignmentName = Guid.NewGuid().ToString();

        return Program.Main([
            "role",
            "assignment",
            "create",
            "--name",
            roleAssignmentName,
            "--role-definition-id",
            roleDefinitionId,
            "--principal-id",
            principalId.ToString(),
            "--principal-type",
            "ServicePrincipal",
            "--scope",
            scope,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
    }
}