using Azure;
using Azure.Containers.ContainerRegistry;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerRegistry;
using Azure.ResourceManager.ContainerRegistry.Models;
using Azure.ResourceManager.ManagedServiceIdentities;
using Azure.ResourceManager.Models;
using Microsoft.Graph;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

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
    public async Task ContainerRegistry_GenerateCredentials_ShouldReturnUsernameAndPasswords()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Basic));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);
        var registry = await registries.GetAsync(RegistryName);

        var tokenId = new ResourceIdentifier(
            $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{RegistryName}/tokens/myToken");
        var content = new ContainerRegistryGenerateCredentialsContent
        {
            TokenId = tokenId,
            ExpireOn = DateTimeOffset.UtcNow.AddDays(30)
        };

        // Act
        var result = await registry.Value.GenerateCredentialsAsync(WaitUntil.Completed, content);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Username, Is.EqualTo("myToken"));
            Assert.That(result.Value.Passwords, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Value.Passwords[0].Value, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Value.Passwords[0].ExpireOn, Is.Not.Null);
        });
    }

    [Test]
    public async Task ContainerRegistry_GenerateCredentials_WithSpecificPasswordName_ShouldReturnSinglePassword()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Basic));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);
        var registry = await registries.GetAsync(RegistryName);

        var tokenId = new ResourceIdentifier(
            $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{RegistryName}/tokens/myToken");
        var content = new ContainerRegistryGenerateCredentialsContent
        {
            TokenId = tokenId,
            Name = ContainerRegistryTokenPasswordName.Password1
        };

        // Act
        var result = await registry.Value.GenerateCredentialsAsync(WaitUntil.Completed, content);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Username, Is.EqualTo("myToken"));
            Assert.That(result.Value.Passwords, Has.Count.EqualTo(1));
            Assert.That(result.Value.Passwords[0].Name, Is.EqualTo(ContainerRegistryTokenPasswordName.Password1));
        });
    }

    [Test]
    public async Task ContainerRegistry_GenerateCredentials_ForNonExistentRegistry_ShouldFail()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var registryResourceId = ContainerRegistryResource.CreateResourceIdentifier(
            SubscriptionId.ToString(), ResourceGroupName, "nonexistentregistry99");
        var registry = armClient.GetContainerRegistryResource(registryResourceId);

        var content = new ContainerRegistryGenerateCredentialsContent();

        // Act & Assert
        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await registry.GenerateCredentialsAsync(WaitUntil.Completed, content));
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

    [Test]
    public async Task ContainerRegistry_RegenerateCredential_ShouldReturnNewPassword()
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

        // capture the original password
        var originalCredentials = await registry.Value.GetCredentialsAsync();
        var originalPassword = originalCredentials.Value.Passwords
            .First(p => p.Name == ContainerRegistryPasswordName.Password).Value;

        // Act — regenerate password
        var content = new ContainerRegistryCredentialRegenerateContent(ContainerRegistryPasswordName.Password);
        var result = await registry.Value.RegenerateCredentialAsync(content);

        // Assert — username preserved, password rotated
        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Username, Is.EqualTo(RegistryName));
            Assert.That(result.Value.Passwords, Is.Not.Null.And.Not.Empty);

            var newPassword = result.Value.Passwords
                .First(p => p.Name == ContainerRegistryPasswordName.Password).Value;
            Assert.That(newPassword, Is.Not.EqualTo(originalPassword), "Password should have changed after regeneration.");
        });
    }

    [Test]
    public async Task ContainerRegistry_RegenerateCredential_Password2_ShouldReturnNewPassword2()
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

        var originalCredentials = await registry.Value.GetCredentialsAsync();
        var originalPassword2 = originalCredentials.Value.Passwords
            .First(p => p.Name == ContainerRegistryPasswordName.Password2).Value;

        // Act
        var content = new ContainerRegistryCredentialRegenerateContent(ContainerRegistryPasswordName.Password2);
        var result = await registry.Value.RegenerateCredentialAsync(content);

        // Assert
        var newPassword2 = result.Value.Passwords
            .First(p => p.Name == ContainerRegistryPasswordName.Password2).Value;
        Assert.That(newPassword2, Is.Not.EqualTo(originalPassword2), "Password2 should have changed after regeneration.");
    }

    [Test]
    public async Task ContainerRegistry_RegenerateCredential_WhenAdminDisabled_ShouldFail()
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

        var content = new ContainerRegistryCredentialRegenerateContent(ContainerRegistryPasswordName.Password);

        // Act & Assert
        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await registry.Value.RegenerateCredentialAsync(content));
    }

    [Test]
    public async Task ContainerRegistry_ListUsages_BasicSku_ShouldReturnExpectedLimits()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Basic));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);
        var registry = await registries.GetAsync(RegistryName);

        // Act
        var usages = new List<Azure.ResourceManager.ContainerRegistry.Models.ContainerRegistryUsage>();
        await foreach (var u in registry.Value.GetUsagesAsync())
            usages.Add(u);

        // Assert
        var size = usages.FirstOrDefault(u => u.Name == "Size");
        var webhooks = usages.FirstOrDefault(u => u.Name == "Webhooks");

        Assert.Multiple(() =>
        {
            Assert.That(size, Is.Not.Null);
            Assert.That(size!.Limit, Is.EqualTo(10737418240L));
            Assert.That(size.CurrentValue, Is.EqualTo(0));
            Assert.That(webhooks, Is.Not.Null);
            Assert.That(webhooks!.Limit, Is.EqualTo(2L));
        });
    }

    [Test]
    public async Task ContainerRegistry_ListUsages_PremiumSku_ShouldReturnHigherLimits()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Premium));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);
        var registry = await registries.GetAsync(RegistryName);

        // Act
        var usages = new List<Azure.ResourceManager.ContainerRegistry.Models.ContainerRegistryUsage>();
        await foreach (var u in registry.Value.GetUsagesAsync())
            usages.Add(u);

        // Assert
        var size = usages.FirstOrDefault(u => u.Name == "Size");
        var webhooks = usages.FirstOrDefault(u => u.Name == "Webhooks");

        Assert.Multiple(() =>
        {
            Assert.That(size, Is.Not.Null);
            Assert.That(size!.Limit, Is.EqualTo(536870912000L));
            Assert.That(webhooks, Is.Not.Null);
            Assert.That(webhooks!.Limit, Is.EqualTo(500L));
        });
    }

    [Test]
    public async Task ContainerRegistry_ListRepositories_ShouldReflectPushedRepository()
    {
        // Arrange — create registry via ARM
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(
            new AzureLocation("westeurope"),
            new ContainerRegistrySku(ContainerRegistrySkuName.Basic));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        var host = TopazResourceHelpers.GetContainerRegistryLoginServer(RegistryName);

        using var http = new HttpClient();

        var catalogUri = $"https://{host}/v2/_catalog";

        // Act 1 — catalog should be empty for a fresh registry
        var req1 = new HttpRequestMessage(HttpMethod.Get, catalogUri);
        var resp1 = await http.SendAsync(req1);
        var body1 = JsonDocument.Parse(await resp1.Content.ReadAsStringAsync());

        Assert.That(body1.RootElement.GetProperty("repositories").GetArrayLength(), Is.EqualTo(0));

        // Act 2 — push a minimal manifest to create a repository on the data plane
        const string repoName = "my-app";
        const string minimalManifest =
            """{"schemaVersion":2,"mediaType":"application/vnd.docker.distribution.manifest.v2+json","""
            + "\"config\":{\"mediaType\":\"application/vnd.docker.container.image.v1+json\",\"size\":0,"
            + "\"digest\":\"sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\"},"
            + "\"layers\":[]}";

        var putUri = $"https://{host}/v2/{repoName}/manifests/v1";
        var req2 = new HttpRequestMessage(HttpMethod.Put, putUri);
        req2.Content = new StringContent(
            minimalManifest, Encoding.UTF8,
            "application/vnd.docker.distribution.manifest.v2+json");
        var resp2 = await http.SendAsync(req2);

        Assert.That((int)resp2.StatusCode, Is.EqualTo(201),
            $"PUT manifest failed: {await resp2.Content.ReadAsStringAsync()}");

        // Act 3 — catalog should now list the pushed repository
        var req3 = new HttpRequestMessage(HttpMethod.Get, catalogUri);
        var resp3 = await http.SendAsync(req3);
        var body3 = JsonDocument.Parse(await resp3.Content.ReadAsStringAsync());

        var repos = body3.RootElement.GetProperty("repositories")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();

        Assert.That(repos, Contains.Item(repoName));
    }

    [Test]
    public async Task ContainerRegistry_ListRepositories_SdkClient_ShouldReturnPushedRepository()
    {
        // Arrange — create registry via ARM
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(
            new AzureLocation("westeurope"),
            new ContainerRegistrySku(ContainerRegistrySkuName.Basic));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        var host = TopazResourceHelpers.GetContainerRegistryLoginServer(RegistryName);

        // Push a minimal manifest via HttpClient to seed a repository.
        using var http = new HttpClient();

        const string repoName = "sdk-app";
        const string minimalManifest =
            "{\"schemaVersion\":2,\"mediaType\":\"application/vnd.docker.distribution.manifest.v2+json\"," +
            "\"config\":{\"mediaType\":\"application/vnd.docker.container.image.v1+json\",\"size\":0," +
            "\"digest\":\"sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\"}," +
            "\"layers\":[]}";

        var putReq = new HttpRequestMessage(
            HttpMethod.Put,
            $"https://{host}/v2/{repoName}/manifests/v1");
        putReq.Content = new StringContent(
            minimalManifest, Encoding.UTF8,
            "application/vnd.docker.distribution.manifest.v2+json");
        var putResp = await http.SendAsync(putReq);
        Assert.That((int)putResp.StatusCode, Is.EqualTo(201),
            $"PUT manifest failed: {await putResp.Content.ReadAsStringAsync()}");

        // Act — use the Azure SDK ContainerRegistryClient with proper auth wiring.
        //
        // ContainerRegistryClient follows a 3-leg OAuth2 exchange:
        //   1. GET /v2/ → 401 Bearer challenge with realm URL
        //   2. POST /oauth2/exchange (AAD token → ACR refresh token)
        //   3. POST /oauth2/token (refresh token → ACR access token)
        var loginServer = new Uri($"https://{host}");
        var registryClient = new ContainerRegistryClient(loginServer, credential);

        var repos = new List<string>();
        await foreach (var repo in registryClient.GetRepositoryNamesAsync())
            repos.Add(repo);

        // Assert
        Assert.That(repos, Contains.Item(repoName));
    }

    [Test]
    public async Task ContainerRegistry_ListTags_ShouldReturnPushedTags()
    {
        // Arrange — create registry via ARM
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(
            new AzureLocation("westeurope"),
            new ContainerRegistrySku(ContainerRegistrySkuName.Basic));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        var host = TopazResourceHelpers.GetContainerRegistryLoginServer(RegistryName);
        using var http = new HttpClient();

        const string repoName = "tag-test-app";
        const string minimalManifest =
            """{"schemaVersion":2,"mediaType":"application/vnd.docker.distribution.manifest.v2+json","""
            + "\"config\":{\"mediaType\":\"application/vnd.docker.container.image.v1+json\",\"size\":0,"
            + "\"digest\":\"sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\"},"
            + "\"layers\":[]}";

        // Push three tags
        foreach (var tag in new[] { "v1", "v2", "latest" })
        {
            var req = new HttpRequestMessage(HttpMethod.Put, $"https://{host}/v2/{repoName}/manifests/{tag}");
            req.Content = new StringContent(minimalManifest, Encoding.UTF8,
                "application/vnd.docker.distribution.manifest.v2+json");
            var resp = await http.SendAsync(req);
            Assert.That((int)resp.StatusCode, Is.EqualTo(201),
                $"PUT manifest/{tag} failed: {await resp.Content.ReadAsStringAsync()}");
        }

        // Act — GET /v2/{repo}/tags/list
        var tagsResp = await http.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, $"https://{host}/v2/{repoName}/tags/list"));
        Assert.That((int)tagsResp.StatusCode, Is.EqualTo(200));

        var body = JsonDocument.Parse(await tagsResp.Content.ReadAsStringAsync());
        var tags = body.RootElement.GetProperty("tags")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();

        // Assert — all three tags returned; digest-indexed copies excluded
        Assert.Multiple(() =>
        {
            Assert.That(tags, Contains.Item("v1"));
            Assert.That(tags, Contains.Item("v2"));
            Assert.That(tags, Contains.Item("latest"));
            Assert.That(tags, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public async Task ContainerRegistry_ListTags_WithPagination_ShouldRespectNAndLast()
    {
        // Arrange — create registry and push several tags
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(
            new AzureLocation("westeurope"),
            new ContainerRegistrySku(ContainerRegistrySkuName.Basic));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        var host = TopazResourceHelpers.GetContainerRegistryLoginServer(RegistryName);
        using var http = new HttpClient();

        const string repoName = "pagination-app";
        const string minimalManifest =
            """{"schemaVersion":2,"mediaType":"application/vnd.docker.distribution.manifest.v2+json","""
            + "\"config\":{\"mediaType\":\"application/vnd.docker.container.image.v1+json\",\"size\":0,"
            + "\"digest\":\"sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\"},"
            + "\"layers\":[]}";

        foreach (var tag in new[] { "v1", "v2", "v3", "v4", "v5" })
        {
            var req = new HttpRequestMessage(HttpMethod.Put, $"https://{host}/v2/{repoName}/manifests/{tag}");
            req.Content = new StringContent(minimalManifest, Encoding.UTF8,
                "application/vnd.docker.distribution.manifest.v2+json");
            await http.SendAsync(req);
        }

        // Act — first page: n=2 → ["v1","v2"]
        var page1Resp = await http.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, $"https://{host}/v2/{repoName}/tags/list?n=2"));
        var page1 = JsonDocument.Parse(await page1Resp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("tags").EnumerateArray().Select(e => e.GetString()).ToList();

        // Act — second page: n=2&last=v2 → ["v3","v4"]
        var page2Resp = await http.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, $"https://{host}/v2/{repoName}/tags/list?n=2&last=v2"));
        var page2 = JsonDocument.Parse(await page2Resp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("tags").EnumerateArray().Select(e => e.GetString()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(page1, Is.EqualTo(new[] { "v1", "v2" }));
            Assert.That(page2, Is.EqualTo(new[] { "v3", "v4" }));
        });
    }

    [Test]
    public async Task ContainerRegistry_DeleteManifest_ShouldReturn202AndManifestShouldBeGone()
    {
        // Arrange — create registry via ARM
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(
            new AzureLocation("westeurope"),
            new ContainerRegistrySku(ContainerRegistrySkuName.Basic));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        var host = TopazResourceHelpers.GetContainerRegistryLoginServer(RegistryName);
        using var http = new HttpClient();

        const string repoName = "delete-manifest-app";
        const string minimalManifest =
            """{"schemaVersion":2,"mediaType":"application/vnd.docker.distribution.manifest.v2+json","""
            + "\"config\":{\"mediaType\":\"application/vnd.docker.container.image.v1+json\",\"size\":0,"
            + "\"digest\":\"sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\"},"
            + "\"layers\":[]}";

        // Push a manifest under tag "v1"
        var putReq = new HttpRequestMessage(HttpMethod.Put, $"https://{host}/v2/{repoName}/manifests/v1");
        putReq.Content = new StringContent(minimalManifest, Encoding.UTF8,
            "application/vnd.docker.distribution.manifest.v2+json");
        var putResp = await http.SendAsync(putReq);
        Assert.That((int)putResp.StatusCode, Is.EqualTo(201),
            $"PUT manifest failed: {await putResp.Content.ReadAsStringAsync()}");

        // Act — delete by tag
        var deleteResp = await http.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, $"https://{host}/v2/{repoName}/manifests/v1"));
        Assert.That((int)deleteResp.StatusCode, Is.EqualTo(202),
            $"DELETE manifest failed: {await deleteResp.Content.ReadAsStringAsync()}");

        // Assert — GET should now return 404
        var getResp = await http.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, $"https://{host}/v2/{repoName}/manifests/v1"));
        Assert.That((int)getResp.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task ContainerRegistry_DeleteManifest_NotFound_ShouldReturn404()
    {
        // Arrange — create registry via ARM
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(
            new AzureLocation("westeurope"),
            new ContainerRegistrySku(ContainerRegistrySkuName.Basic));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        var host = TopazResourceHelpers.GetContainerRegistryLoginServer(RegistryName);
        using var http = new HttpClient();

        // Act — delete a manifest that was never pushed
        var deleteResp = await http.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, $"https://{host}/v2/nonexistent-repo/manifests/v1"));
        Assert.That((int)deleteResp.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task ContainerRegistry_HeadManifest_ExistingManifest_ShouldReturn200WithHeaders()
    {
        // Arrange — create registry via ARM
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(
            new AzureLocation("westeurope"),
            new ContainerRegistrySku(ContainerRegistrySkuName.Basic));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        var host = TopazResourceHelpers.GetContainerRegistryLoginServer(RegistryName);
        using var http = new HttpClient();

        const string repoName = "head-manifest-app";
        const string minimalManifest =
            """{"schemaVersion":2,"mediaType":"application/vnd.docker.distribution.manifest.v2+json","""
            + "\"config\":{\"mediaType\":\"application/vnd.docker.container.image.v1+json\",\"size\":0,"
            + "\"digest\":\"sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\"},"
            + "\"layers\":[]}";

        // Push a manifest under tag "v1"
        var putReq = new HttpRequestMessage(HttpMethod.Put, $"https://{host}/v2/{repoName}/manifests/v1");
        putReq.Content = new StringContent(minimalManifest, Encoding.UTF8,
            "application/vnd.docker.distribution.manifest.v2+json");
        var putResp = await http.SendAsync(putReq);
        Assert.That((int)putResp.StatusCode, Is.EqualTo(201),
            $"PUT manifest failed: {await putResp.Content.ReadAsStringAsync()}");

        // Act — HEAD the manifest by tag
        var headResp = await http.SendAsync(
            new HttpRequestMessage(HttpMethod.Head, $"https://{host}/v2/{repoName}/manifests/v1"));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That((int)headResp.StatusCode, Is.EqualTo(200));
            Assert.That(headResp.Headers.Contains("Docker-Content-Digest"), Is.True);
            Assert.That(headResp.Content.Headers.ContentLength, Is.GreaterThan(0));
            Assert.That(headResp.Content.Headers.ContentType?.MediaType,
                Is.EqualTo("application/vnd.docker.distribution.manifest.v2+json"));
        });

        // Assert — body must be empty for HEAD
        var body = await headResp.Content.ReadAsByteArrayAsync();
        Assert.That(body, Is.Empty);
    }

    [Test]
    public async Task ContainerRegistry_HeadManifest_ByDigest_ShouldReturn200()
    {
        // Arrange — create registry via ARM
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(
            new AzureLocation("westeurope"),
            new ContainerRegistrySku(ContainerRegistrySkuName.Basic));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        var host = TopazResourceHelpers.GetContainerRegistryLoginServer(RegistryName);
        using var http = new HttpClient();

        const string repoName = "head-manifest-digest-app";
        const string minimalManifest =
            """{"schemaVersion":2,"mediaType":"application/vnd.docker.distribution.manifest.v2+json","""
            + "\"config\":{\"mediaType\":\"application/vnd.docker.container.image.v1+json\",\"size\":0,"
            + "\"digest\":\"sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\"},"
            + "\"layers\":[]}";

        // Push a manifest and capture the returned digest
        var putReq = new HttpRequestMessage(HttpMethod.Put, $"https://{host}/v2/{repoName}/manifests/v1");
        putReq.Content = new StringContent(minimalManifest, Encoding.UTF8,
            "application/vnd.docker.distribution.manifest.v2+json");
        var putResp = await http.SendAsync(putReq);
        Assert.That((int)putResp.StatusCode, Is.EqualTo(201));
        var digest = putResp.Headers.GetValues("Docker-Content-Digest").Single();

        // Act — HEAD by digest
        var headResp = await http.SendAsync(
            new HttpRequestMessage(HttpMethod.Head, $"https://{host}/v2/{repoName}/manifests/{digest}"));

        Assert.Multiple(() =>
        {
            Assert.That((int)headResp.StatusCode, Is.EqualTo(200));
            Assert.That(headResp.Headers.GetValues("Docker-Content-Digest").Single(), Is.EqualTo(digest));
        });
    }

    [Test]
    public async Task ContainerRegistry_HeadManifest_NotFound_ShouldReturn404()
    {
        // Arrange — create registry via ARM
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(
            new AzureLocation("westeurope"),
            new ContainerRegistrySku(ContainerRegistrySkuName.Basic));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        var host = TopazResourceHelpers.GetContainerRegistryLoginServer(RegistryName);
        using var http = new HttpClient();

        // Act — HEAD a manifest that was never pushed
        var headResp = await http.SendAsync(
            new HttpRequestMessage(HttpMethod.Head, $"https://{host}/v2/nonexistent-repo/manifests/v1"));

        Assert.That((int)headResp.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task ContainerRegistry_DeleteBlob_ExistingDigest_ShouldReturn202AndBlobShouldBeGone()
    {
        // Arrange — create registry via ARM
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(
            new AzureLocation("westeurope"),
            new ContainerRegistrySku(ContainerRegistrySkuName.Basic));
        await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        var host = TopazResourceHelpers.GetContainerRegistryLoginServer(RegistryName);
        using var http = new HttpClient();

        const string repoName = "delete-blob-app";
        var payload = Encoding.UTF8.GetBytes("delete-blob-payload");
        var digest = "sha256:" + Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        // Start upload session.
        var initiateResp = await http.SendAsync(
            new HttpRequestMessage(HttpMethod.Post, $"https://{host}/v2/{repoName}/blobs/uploads/"));
        Assert.That((int)initiateResp.StatusCode, Is.EqualTo(202));

        var uploadLocation = initiateResp.Headers.Location;
        Assert.That(uploadLocation, Is.Not.Null);

        // Complete upload with a monolithic payload.
        var completeReq = new HttpRequestMessage(HttpMethod.Put, $"{uploadLocation}?digest={digest}")
        {
            Content = new ByteArrayContent(payload)
        };
        completeReq.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var completeResp = await http.SendAsync(completeReq);
        Assert.That((int)completeResp.StatusCode, Is.EqualTo(201),
            $"PUT blob failed: {await completeResp.Content.ReadAsStringAsync()}");

        // Act — delete the blob by digest.
        var deleteResp = await http.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, $"https://{host}/v2/{repoName}/blobs/{digest}"));

        // Assert — deletion accepted and blob no longer addressable.
        Assert.That((int)deleteResp.StatusCode, Is.EqualTo(202),
            $"DELETE blob failed: {await deleteResp.Content.ReadAsStringAsync()}");

        var headResp = await http.SendAsync(
            new HttpRequestMessage(HttpMethod.Head, $"https://{host}/v2/{repoName}/blobs/{digest}"));
        Assert.That((int)headResp.StatusCode, Is.EqualTo(404));
    }
}
