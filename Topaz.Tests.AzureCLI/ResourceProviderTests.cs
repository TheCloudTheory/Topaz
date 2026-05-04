namespace Topaz.Tests.AzureCLI;

public class ResourceProviderTests : TopazFixture
{
    [Test]
    public async Task ResourceProviderTests_WhenListProvidersIsCalled_ItShouldReturnKnownProviders()
    {
        await RunAzureCliCommand("az provider list", result =>
        {
            var namespaces = result.AsArray()
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

            foreach (var entry in result.AsArray())
            {
                Assert.That(entry!["registrationState"]?.GetValue<string>(), Is.Not.Null.And.Not.Empty,
                    $"registrationState missing for {entry["namespace"]}");
            }
        });
    }

    [Test]
    public async Task ResourceProviderTests_WhenShowProviderIsCalled_ItShouldReturnRegistrationState()
    {
        await RunAzureCliCommand("az provider show --namespace Microsoft.KeyVault", result =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(result["namespace"]!.GetValue<string>(), Is.EqualTo("Microsoft.KeyVault"));
                Assert.That(result["registrationState"]!.GetValue<string>(), Is.EqualTo("Registered"));
            });
        });
    }

    [Test]
    public async Task ResourceProviderTests_WhenProviderIsUnregisteredAndReRegistered_RegistrationStateChanges()
    {
        // Unregister
        await RunAzureCliCommand("az provider unregister --namespace Microsoft.Storage --wait");

        await RunAzureCliCommand("az provider show --namespace Microsoft.Storage", result =>
        {
            Assert.That(result["registrationState"]!.GetValue<string>(), Is.EqualTo("Unregistered"));
        });

        // Also verify the list reflects the new state
        await RunAzureCliCommand("az provider list", result =>
        {
            var storageEntry = result.AsArray()
                .First(v => string.Equals(v!["namespace"]!.GetValue<string>(), "Microsoft.Storage", StringComparison.OrdinalIgnoreCase));
            Assert.That(storageEntry!["registrationState"]!.GetValue<string>(), Is.EqualTo("Unregistered"));
        });

        // Re-register
        await RunAzureCliCommand("az provider register --namespace Microsoft.Storage --wait");

        await RunAzureCliCommand("az provider show --namespace Microsoft.Storage", result =>
        {
            Assert.That(result["registrationState"]!.GetValue<string>(), Is.EqualTo("Registered"));
        });
    }

    [Test]
    public async Task ResourceProviderTests_WhenUnregisteredProviderIsUsed_CliAutoRegistersAndSucceeds()
    {
        // Unregister Microsoft.KeyVault
        await RunAzureCliCommand("az provider unregister --namespace Microsoft.KeyVault --wait");

        await RunAzureCliCommand("az provider show --namespace Microsoft.KeyVault", result =>
        {
            Assert.That(result["registrationState"]!.GetValue<string>(), Is.EqualTo("Unregistered"));
        });

        // Attempt to create a Key Vault while the provider is unregistered.
        // Azure CLI detects the 409 MissingSubscriptionRegistration response, automatically
        // registers the provider, and retries — so the command should ultimately succeed (exit 0).
        await RunAzureCliCommand("az group create -n rg-provider-gate-cli -l westeurope");
        await RunAzureCliCommand(
            "az keyvault create --name kv-provider-gate --resource-group rg-provider-gate-cli --location westeurope --sku standard",
            result =>
            {
                Assert.That(result["name"]!.GetValue<string>(), Is.EqualTo("kv-provider-gate"));
                Assert.That(result["properties"]!["vaultUri"]!.GetValue<string>(),
                    Does.Contain("kv-provider-gate"));
            });

        // The provider should now be registered again after the CLI auto-registration
        await RunAzureCliCommand("az provider show --namespace Microsoft.KeyVault", result =>
        {
            Assert.That(result["registrationState"]!.GetValue<string>(), Is.EqualTo("Registered"));
        });

        // Cleanup
        await RunAzureCliCommand("az group delete -n rg-provider-gate-cli --yes");
    }
}
