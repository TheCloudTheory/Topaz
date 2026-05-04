using System.Net;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ResourceProviderTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");

    private const string SubscriptionName = "sub-provider-tests";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
    }

    [Test]
    public async Task ResourceProvider_List_ReturnsAllKnownProviders()
    {
        // Arrange
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);

        // Act
        var result = await topaz.ListProvidersAsync(SubscriptionId);
        var values = result["value"]!.AsArray();

        // Assert
        Assert.That(values.Count, Is.GreaterThanOrEqualTo(10));

        var namespaces = values
            .Select(v => v!["namespace"]!.GetValue<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Multiple(() =>
        {
            Assert.That(namespaces, Does.Contain("Microsoft.KeyVault"));
            Assert.That(namespaces, Does.Contain("Microsoft.Storage"));
            Assert.That(namespaces, Does.Contain("Microsoft.ContainerRegistry"));
            Assert.That(namespaces, Does.Contain("Microsoft.ServiceBus"));
            Assert.That(namespaces, Does.Contain("Microsoft.EventHub"));
        });

        foreach (var value in values)
        {
            Assert.That(value!["registrationState"]!.GetValue<string>(), Is.Not.Null.And.Not.Empty,
                $"registrationState missing for namespace {value["namespace"]}");
            Assert.That(value["registrationPolicy"]!.GetValue<string>(), Is.Not.Null.And.Not.Empty,
                $"registrationPolicy missing for namespace {value["namespace"]}");
        }
    }

    [Test]
    public async Task ResourceProvider_Get_ReflectsRegistrationState()
    {
        // Arrange
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);

        // Act
        var result = await topaz.GetProviderAsync(SubscriptionId, "Microsoft.KeyVault");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result["namespace"]!.GetValue<string>(), Is.EqualTo("Microsoft.KeyVault"));
            Assert.That(result["registrationState"]!.GetValue<string>(), Is.EqualTo("Registered"));
            Assert.That(result["registrationPolicy"]!.GetValue<string>(), Is.EqualTo("RegistrationRequired"));
        });
    }

    [Test]
    public async Task ResourceProvider_RegisterAndUnregister_ChangesRegistrationState()
    {
        // Arrange
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);

        // Act — unregister
        var unregisterResponse = await topaz.UnregisterProviderAsync(SubscriptionId, "Microsoft.Storage");
        Assert.That(unregisterResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var afterUnregister = await topaz.GetProviderAsync(SubscriptionId, "Microsoft.Storage");
        Assert.That(afterUnregister["registrationState"]!.GetValue<string>(), Is.EqualTo("Unregistered"));

        // Also check the list reflects the unregistered state
        var list = await topaz.ListProvidersAsync(SubscriptionId);
        var storageInList = list["value"]!.AsArray()
            .First(v => string.Equals(v!["namespace"]!.GetValue<string>(), "Microsoft.Storage", StringComparison.OrdinalIgnoreCase));
        Assert.That(storageInList!["registrationState"]!.GetValue<string>(), Is.EqualTo("Unregistered"));

        // Act — re-register
        var registerResponse = await topaz.RegisterProviderAsync(SubscriptionId, "Microsoft.Storage");
        Assert.That(registerResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var afterRegister = await topaz.GetProviderAsync(SubscriptionId, "Microsoft.Storage");
        Assert.That(afterRegister["registrationState"]!.GetValue<string>(), Is.EqualTo("Registered"));
    }

    [Test]
    public async Task ResourceProvider_ActionOnUnregisteredProvider_Returns409()
    {
        // Arrange — create a resource group, then unregister Microsoft.KeyVault
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        var armClient = new ArmClient(credentials, SubscriptionId.ToString(), ArmClientOptions);

        await Program.RunAsync([
            "group", "create",
            "--name", "rg-provider-gate",
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await topaz.UnregisterProviderAsync(SubscriptionId, "Microsoft.KeyVault");

        // Act — attempt to create a Key Vault while the provider is unregistered.
        // The enforcement gate in the Router should reject this before the endpoint runs.
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroupAsync("rg-provider-gate");

        var ex = Assert.ThrowsAsync<RequestFailedException>(async () =>
        {
            await rg.Value.GetKeyVaults().CreateOrUpdateAsync(
                WaitUntil.Started,
                "kv-provider-gate-test",
                new KeyVaultCreateOrUpdateContent(
                    AzureLocation.WestEurope,
                    new KeyVaultProperties(
                        Guid.Parse("00000000-0000-0000-0000-000000000001"),
                        new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))));
        });

        // Assert — 409 Conflict with MissingSubscriptionRegistration
        Assert.That(ex!.Status, Is.EqualTo(409));
        Assert.That(ex.ErrorCode, Is.EqualTo("MissingSubscriptionRegistration"));

        // Cleanup — re-register so other tests are not affected
        await topaz.RegisterProviderAsync(SubscriptionId, "Microsoft.KeyVault");
    }
}
