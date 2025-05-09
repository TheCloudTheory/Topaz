using Azure.Local.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Azure.Local.Tests.E2E;

public class KeyVaultTests
{
    private const string VaultUrl = "https://localhost:8900";

    [Test]
    public void KeyVaultTests_WhenSecretIsCreated_ItShouldBePossibleToFetch()
    {
        // Arrange
        var credentials = new AzureLocalCredential();
        var client = new SecretClient(vaultUri: new Uri(VaultUrl), credential: credentials);

        // Act
        var createSecret = client.SetSecret("secret-name", "test");
        var secret = client.GetSecret("secret-name");

        // Assert
        Assert.That(secret.Value.Value, Is.EqualTo("test"));
    }
}
