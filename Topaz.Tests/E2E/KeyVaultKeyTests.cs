using Topaz.CLI;
using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.Security.KeyVault.Keys;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class KeyVaultKeyTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A3C21F10-5B7E-4D22-9ABF-6E30F7A11C42");

    private const string SubscriptionName = "sub-kv-keys-test";
    private const string ResourceGroupName = "test-kv-keys";
    private const string TestKeyVaultName = "testkvkeys";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
    }

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync(["keyvault", "delete", "--resource-group", ResourceGroupName,
            "--name", TestKeyVaultName, "--subscription-id", SubscriptionId.ToString()]);
    }

    private KeyClient CreateKeyClient()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        return new KeyClient(
            vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(TestKeyVaultName),
            credential: credential,
            new KeyClientOptions { DisableChallengeResourceVerification = true });
    }

    private void EnsureVault()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(
            Azure.Core.AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        resourceGroup.Value.GetKeyVaults().CreateOrUpdate(WaitUntil.Completed, TestKeyVaultName, operation);
    }

    [Test]
    public void KeyVaultKeyTests_CreateRsaKey_ShouldReturnValidKeyBundle()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act
        var result = client.CreateRsaKey(new CreateRsaKeyOptions("my-rsa-key"));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.Name, Is.EqualTo("my-rsa-key"));
            Assert.That(result.Value.KeyType, Is.EqualTo(KeyType.Rsa));
            Assert.That(result.Value.Id, Is.Not.Null);
            Assert.That(result.Value.Key.N, Is.Not.Null);
            Assert.That(result.Value.Key.E, Is.Not.Null);
        });
    }

    [Test]
    public void KeyVaultKeyTests_CreateEcKey_ShouldReturnValidKeyBundle()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act
        var result = client.CreateEcKey(new CreateEcKeyOptions("my-ec-key")
        {
            CurveName = KeyCurveName.P256
        });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.Name, Is.EqualTo("my-ec-key"));
            Assert.That(result.Value.KeyType, Is.EqualTo(KeyType.Ec));
            Assert.That(result.Value.Key.CurveName, Is.EqualTo(KeyCurveName.P256));
            Assert.That(result.Value.Key.X, Is.Not.Null);
            Assert.That(result.Value.Key.Y, Is.Not.Null);
        });
    }

    [Test]
    public void KeyVaultKeyTests_CreateRsaKeyWithSize_ShouldUseRequestedKeySize()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act
        var result = client.CreateRsaKey(new CreateRsaKeyOptions("my-rsa-4096") { KeySize = 4096 });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.Name, Is.EqualTo("my-rsa-4096"));
            Assert.That(result.Value.Key.N, Is.Not.Null);
            // 4096-bit key → modulus is 512 bytes → ~683 base64url chars
            Assert.That(result.Value.Key.N!.Length, Is.GreaterThan(500));
        });
    }

    [Test]
    public void KeyVaultKeyTests_CreateKeyMultipleTimes_ShouldCreateNewVersions()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act
        var v1 = client.CreateRsaKey(new CreateRsaKeyOptions("versioned-key"));
        var v2 = client.CreateRsaKey(new CreateRsaKeyOptions("versioned-key"));

        // Assert — each call returns a different version (different kid)
        Assert.That(v1.Value.Id.ToString(), Is.Not.EqualTo(v2.Value.Id.ToString()));
    }

    [Test]
    public void KeyVaultKeyTests_CreateKeyIdContainsVaultAndKeyName()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act
        var result = client.CreateRsaKey(new CreateRsaKeyOptions("id-check-key"));

        // Assert
        var kid = result.Value.Id.ToString();
        Assert.Multiple(() =>
        {
            Assert.That(kid, Does.Contain(TestKeyVaultName));
            Assert.That(kid, Does.Contain("keys"));
            Assert.That(kid, Does.Contain("id-check-key"));
        });
    }
}
