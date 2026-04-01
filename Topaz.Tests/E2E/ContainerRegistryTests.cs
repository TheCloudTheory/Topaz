using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerRegistry;
using Azure.ResourceManager.ContainerRegistry.Models;
using Azure.ResourceManager.ManagedServiceIdentities;
using Azure.ResourceManager.Models;
using Microsoft.Graph;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ContainerRegistryTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("E1F2A3B4-C5D6-E7F8-A9B0-C1D2E3F4A5B6");

    private static GraphServiceClient GraphClient => new(new HttpClient(), new LocalGraphAuthenticationProvider(),
        "https://topaz.local.dev:8899");

    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test-acr";
    private const string RegistryName = "topazacr01";

    [SetUp]
    public async Task SetUp()
    {
        await Program.Main(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.Main(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);

        await Program.Main(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.Main(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
    }

    [TearDown]
    public async Task TearDown()
    {
        await Program.Main(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
    }

    [Test]
    public async Task ContainerRegistry_CreateAndGet_ShouldSucceed()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var registries = resourceGroup.Value.GetContainerRegistries();
        var registryData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Basic));

        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        var registry = await registries.GetAsync(RegistryName);

        Assert.Multiple(() =>
        {
            Assert.That(registry.Value.Data.Name, Is.EqualTo(RegistryName));
            Assert.That(registry.Value.Data.LoginServer, Is.Not.Null.And.Not.Empty);
            Assert.That(registry.Value.Data.Sku.Name, Is.EqualTo(ContainerRegistrySkuName.Basic));
        });
    }

    [Test]
    public async Task ContainerRegistry_Update_ShouldModifyAdminUserEnabled()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Standard))
        {
            IsAdminUserEnabled = false
        };

        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        var created = await registries.GetAsync(RegistryName);
        Assert.That(created.Value.Data.IsAdminUserEnabled, Is.False);

        var updateData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Standard))
        {
            IsAdminUserEnabled = true
        };
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, updateData);

        var updated = await registries.GetAsync(RegistryName);
        Assert.That(updated.Value.Data.IsAdminUserEnabled, Is.True);
    }

    [Test]
    public async Task ContainerRegistry_Delete_ShouldRemoveRegistry()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Basic));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        var registry = await registries.GetAsync(RegistryName);
        await registry.Value.DeleteAsync(WaitUntil.Completed);

        Assert.ThrowsAsync<RequestFailedException>(async () => await registries.GetAsync(RegistryName));
    }

    [Test]
    public async Task ContainerRegistry_ListByResourceGroup_ShouldContainCreatedRegistry()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Basic));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        try
        {
            var found = false;
            await foreach (var r in registries.GetAllAsync())
            {
                if (!string.Equals(r.Data.Name, RegistryName, StringComparison.OrdinalIgnoreCase)) continue;
                found = true;
                break;
            }

            Assert.That(found, Is.True);
        }
        finally
        {
            var registry = await registries.GetAsync(RegistryName);
            await registry.Value.DeleteAsync(WaitUntil.Completed);
        }
    }

    [Test]
    public async Task ContainerRegistry_CreateWithPremiumSku_ShouldHaveCorrectTier()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Premium));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        try
        {
            var registry = await registries.GetAsync(RegistryName);
            Assert.That(registry.Value.Data.Sku.Name, Is.EqualTo(ContainerRegistrySkuName.Premium));
        }
        finally
        {
            var registry = await registries.GetAsync(RegistryName);
            await registry.Value.DeleteAsync(WaitUntil.Completed);
        }
    }

    [Test]
    public async Task ContainerRegistry_CreateWithSystemAssignedIdentity_ShouldProvisionIdentity()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryName = $"topazacr{Guid.NewGuid():N}"[..24];
        var registryData = new ContainerRegistryData(
            new AzureLocation("westeurope"),
            new ContainerRegistrySku(ContainerRegistrySkuName.Basic))
        {
            Identity = new ManagedServiceIdentity(ManagedServiceIdentityType.SystemAssigned)
        };

        // Act
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, registryName, registryData);
        var registry = await registries.GetAsync(registryName);

        try
        {
            // Assert — registry reports identity
            Assert.That(registry.Value.Data.Identity, Is.Not.Null);
            Assert.That(registry.Value.Data.Identity!.PrincipalId, Is.Not.Null);
            Assert.That(registry.Value.Data.Identity!.TenantId, Is.Not.Null);

            // Assert — system-assigned identity endpoint returns the same identity
            var identityResourceId = SystemAssignedIdentityResource.CreateResourceIdentifier(
                registry.Value.Data.Id.ToString());
            var identity = armClient.GetSystemAssignedIdentityResource(identityResourceId).Get();

            Assert.That(identity.Value.Data.PrincipalId.ToString(),
                Is.EqualTo(registry.Value.Data.Identity!.PrincipalId.ToString()));
            Assert.That(identity.Value.Data.TenantId.ToString(),
                Is.EqualTo(registry.Value.Data.Identity!.TenantId.ToString()));
        }
        finally
        {
            var registry2 = await registries.GetAsync(registryName);
            await registry2.Value.DeleteAsync(WaitUntil.Completed);
        }
    }

    [Test]
    public async Task ContainerRegistry_ListCredentials_WhenAdminEnabled_ShouldReturnCredentials()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Basic))
        {
            IsAdminUserEnabled = true
        };
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);
        var registry = await registries.GetAsync(RegistryName);

        // Act
        var credentials = await registry.Value.GetCredentialsAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(credentials.Value.Username, Is.EqualTo(RegistryName));
            Assert.That(credentials.Value.Passwords, Is.Not.Null.And.Not.Empty);
            Assert.That(credentials.Value.Passwords[0].Value, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task ContainerRegistry_ListCredentials_WhenAdminDisabled_ShouldFail()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Basic))
        {
            IsAdminUserEnabled = false
        };
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);
        var registry = await registries.GetAsync(RegistryName);

        // Act & Assert
        Assert.ThrowsAsync<RequestFailedException>(async () => await registry.Value.GetCredentialsAsync());
    }

    [Test]
    public async Task ContainerRegistry_ListCredentials_AfterEnablingAdmin_ShouldReturnCredentials()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        // Create with admin disabled
        var registryData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Basic))
        {
            IsAdminUserEnabled = false
        };
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        // Enable admin
        var updateData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Basic))
        {
            IsAdminUserEnabled = true
        };
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, updateData);
        var registry = await registries.GetAsync(RegistryName);

        // Act
        var credentials = await registry.Value.GetCredentialsAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(credentials.Value.Username, Is.EqualTo(RegistryName));
            Assert.That(credentials.Value.Passwords, Is.Not.Null.And.Not.Empty);
            Assert.That(credentials.Value.Passwords[0].Value, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task ContainerRegistry_CreateWithSystemAssignedIdentity_ServicePrincipalShouldExistInEntra()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryName = $"topazacr{Guid.NewGuid():N}"[..24];
        var registryData = new ContainerRegistryData(
            new AzureLocation("westeurope"),
            new ContainerRegistrySku(ContainerRegistrySkuName.Basic))
        {
            Identity = new ManagedServiceIdentity(ManagedServiceIdentityType.SystemAssigned)
        };

        // Act
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, registryName, registryData);
        var registry = await registries.GetAsync(registryName);

        try
        {
            var principalId = registry.Value.Data.Identity!.PrincipalId!.Value;

            // Assert — the service principal exists in the emulated Entra tenant,
            // accessible by the principalId (which is the SP's object ID)
            var sp = await GraphClient.ServicePrincipals[principalId.ToString()].GetAsync();
            Assert.That(sp, Is.Not.Null);
        }
        finally
        {
            var registry2 = await registries.GetAsync(registryName);
            await registry2.Value.DeleteAsync(WaitUntil.Completed);
        }
    }
}
