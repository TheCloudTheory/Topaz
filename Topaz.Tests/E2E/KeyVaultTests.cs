using Azure;
using Azure.Security.KeyVault.Secrets;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class KeyVaultTests
{
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    
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

        await Program.Main([
            "keyvault",
            "delete",
            "--name",
            "test",
            "-g",
            "rg-test",
            "--subscription-id",
            SubscriptionId.ToString(),
        ]);
        
        await Program.Main([
            "keyvault",
            "create",
            "--name",
            "test",
            "-g",
            "rg-test",
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
    }

    [Test]
    public void KeyVaultTests_WhenSecretIsCreated_ItShouldBePossibleToFetch()
    {
        // Arrange
        var credentials = new AzureLocalCredential();
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint("test"), credential: credentials, new SecretClientOptions
        {
            DisableChallengeResourceVerification = true
        });

        // Act
        var createSecret = client.SetSecret("secret-name", "test");
        var secret = client.GetSecret("secret-name");
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(secret.Value.Value, Is.EqualTo("test"));
            Assert.That(createSecret.Value.Value, Is.EqualTo("test"));
            Assert.That(secret.Value.Id, Is.Not.Null);
        });
    }
    
    [Test]
    public void KeyVaultTests_WhenSecretIsNotCreated_ItShouldNotBePossibleToFetch()
    {
        // Arrange
        var credentials = new AzureLocalCredential();
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint("test"), credential: credentials, new SecretClientOptions()
        {
            DisableChallengeResourceVerification = true
        });
        
        // Assert
        Assert.Throws<RequestFailedException>(() => client.GetSecret("secret-name"));
    }
    
    [Test]
    public void KeyVaultTests_WhenSecretIsCreatedTwice_ItShouldHaveTwoVersions()
    {
        // Arrange
        var credentials = new AzureLocalCredential();
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint("test"), credential: credentials, new SecretClientOptions()
        {
            DisableChallengeResourceVerification = true
        });

        // Act
        var createSecret = client.SetSecret("secret-name", "test");
        var createSecretSecond = client.SetSecret("secret-name", "test2");
        var secret = client.GetSecret("secret-name");
        var originalSecret = client.GetSecret("secret-name", createSecret.Value.Properties.Version);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(secret.Value.Value, Is.EqualTo("test2"));
            Assert.That(createSecret.Value.Value, Is.EqualTo("test"));
            Assert.That(secret.Value.Id, Is.Not.Null);
            Assert.That(createSecretSecond.Value.Id, Is.Not.Null);
            Assert.That(originalSecret.Value.Value, Is.EqualTo("test"));
        });
    }
    
    [Test]
    public void KeyVaultTests_WhenListOfSecretsIsRequested_TheyMustBeReturned()
    {
        // Arrange
        var credentials = new AzureLocalCredential();
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint("test"), credential: credentials, new SecretClientOptions()
        {
            DisableChallengeResourceVerification = true
        });

        // Act
        var secret1 = client.SetSecret("secret-one", "test");
        var secret2 = client.SetSecret("secret-two", "test");
        var secrets = client.GetPropertiesOfSecrets().ToArray();
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(secrets, Has.Length.EqualTo(2));
            Assert.That(secrets[0].Id, Is.Not.Null);
            Assert.That(secrets[1].Id, Is.Not.Null);
        });
    }
    
    [Test]
    public void KeyVaultTests_SecretIsRemoved_ThenItShouldNoLongerBeAvailable()
    {
        // Arrange
        var credentials = new AzureLocalCredential();
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint("test"), credential: credentials, new SecretClientOptions()
        {
            DisableChallengeResourceVerification = true
        });

        // Act
        client.SetSecret("secret-one", "test");
        client.StartDeleteSecret("secret-one");
        
        // Assert
        Assert.Throws<RequestFailedException>(() => client.GetSecret("secret-one"));
    }
}
