using Topaz.CLI;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.Security.KeyVault.Secrets;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Service.Entra;

namespace Topaz.Tests.E2E;

public class KeyVaultFullTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("1898F130-313E-4D49-85AB-5F501F311159");
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string TestKeyVaultName = "testkv";
    
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

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync([
            "keyvault",
            "delete",
            "--resource-group",
            ResourceGroupName,
            "--name",
            TestKeyVaultName,
            "--subscription-id",
            SubscriptionId.ToString(),
        ]);
    }

    [Test]
    public void KeyVaultTests_WhenKeyVaultIsCreatedViaSDKAndSecretsAreCreated_TheyShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup("test");
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(TestKeyVaultName), credential: credential, new SecretClientOptions()
        {
            DisableChallengeResourceVerification = true
        });
        _ = resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, TestKeyVaultName, operation, CancellationToken.None);
        
        // Act
        _ = client.SetSecret("secret-one", "test");
        var secret = client.GetSecret("secret-one");
        
        // Assert
        Assert.That(secret.Value, Is.Not.Null);
        Assert.That(secret.Value.Value, Is.EqualTo("test"));
    }

    [Test]
    public void KeyVaultTests_WhenSecretPropertiesAreUpdated_UpdatedAttributesShouldBeReflected()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(TestKeyVaultName),
            credential: credential, new SecretClientOptions { DisableChallengeResourceVerification = true });
        _ = resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, TestKeyVaultName, operation, CancellationToken.None);

        _ = client.SetSecret("update-me", "original-value");
        var created = client.GetSecret("update-me");

        // Act
        var props = created.Value.Properties;
        props.Enabled = false;
        var updated = client.UpdateSecretProperties(props);

        // Assert
        Assert.That(updated.Value, Is.Not.Null);
        Assert.That(updated.Value.Enabled, Is.False);
    }

    [Test]
    public void KeyVaultTests_WhenSecretHasMultipleVersions_GetSecretVersionsShouldReturnAll()
    {
        // Arrange
        var tenantId = Guid.Parse(EntraService.TenantId);
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(tenantId, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(TestKeyVaultName),
            credential: credential, new SecretClientOptions { DisableChallengeResourceVerification = true });
        _ = resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, TestKeyVaultName, operation, CancellationToken.None);

        // Act — set the same secret three times to create three versions
        _ = client.SetSecret("versioned-secret", "value-v1");
        _ = client.SetSecret("versioned-secret", "value-v2");
        _ = client.SetSecret("versioned-secret", "value-v3");

        var versions = client.GetPropertiesOfSecretVersions("versioned-secret").ToList();

        // Assert
        Assert.That(versions, Has.Count.EqualTo(3));
    }

    [Test]
    public void KeyVaultTests_WhenSecretIsBackedUp_BackupBlobShouldBeReturned()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(TestKeyVaultName),
            credential: credential, new SecretClientOptions { DisableChallengeResourceVerification = true });
        _ = resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, TestKeyVaultName, operation, CancellationToken.None);

        // Act
        _ = client.SetSecret("backup-secret", "backup-value");
        var backup = client.BackupSecret("backup-secret");

        // Assert
        Assert.That(backup.Value, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void KeyVaultTests_WhenSecretIsBackedUpAndRestored_AllVersionsShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(TestKeyVaultName),
            credential: credential, new SecretClientOptions { DisableChallengeResourceVerification = true });
        _ = resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, TestKeyVaultName, operation, CancellationToken.None);

        // Act — create two versions, back up, delete, then restore
        _ = client.SetSecret("restore-secret", "value-v1");
        _ = client.SetSecret("restore-secret", "value-v2");
        var backup = client.BackupSecret("restore-secret");
        client.StartDeleteSecret("restore-secret");
        var restored = client.RestoreSecretBackup(backup.Value);

        // Assert — latest version survives restore
        Assert.That(restored.Value, Is.Not.Null);
        Assert.That(restored.Value.Name, Is.EqualTo("restore-secret"));

        var allVersions = client.GetPropertiesOfSecretVersions("restore-secret").ToList();
        Assert.That(allVersions, Has.Count.EqualTo(2));
    }

    [Test]
    public void KeyVaultTests_WhenDeletedSecretIsPurged_ItShouldNoLongerAppearInDeletedList()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(TestKeyVaultName),
            credential: credential, new SecretClientOptions { DisableChallengeResourceVerification = true });
        _ = resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, TestKeyVaultName, operation, CancellationToken.None);

        _ = client.SetSecret("purge-me", "value");
        client.StartDeleteSecret("purge-me");

        // Act
        client.PurgeDeletedSecret("purge-me");

        // Assert — the secret must not appear in the deleted list after purge
        var deletedSecrets = client.GetDeletedSecrets().ToList();
        Assert.That(deletedSecrets.Any(s => s.Name == "purge-me"), Is.False);
    }
}