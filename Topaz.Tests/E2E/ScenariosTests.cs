using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.ManagedServiceIdentities;
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
}