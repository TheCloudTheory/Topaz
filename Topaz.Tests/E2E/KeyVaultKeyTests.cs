using Topaz.CLI;
using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.Security.KeyVault.Keys;
using System.Security.Cryptography;
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

    [Test]
    public void KeyVaultKeyTests_ImportRsaKey_ShouldReturnValidKeyBundle()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        using var rsa = RSA.Create(2048);
        var jwk = new JsonWebKey(rsa, includePrivateParameters: false);

        // Act
        var result = client.ImportKey(new ImportKeyOptions("imported-rsa-key", jwk));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.Name, Is.EqualTo("imported-rsa-key"));
            Assert.That(result.Value.KeyType, Is.EqualTo(KeyType.Rsa));
            Assert.That(result.Value.Id, Is.Not.Null);
            Assert.That(result.Value.Key.N, Is.Not.Null);
            Assert.That(result.Value.Key.E, Is.Not.Null);
        });
    }

    [Test]
    public void KeyVaultKeyTests_ImportEcKey_ShouldReturnValidKeyBundle()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var jwk = new JsonWebKey(ec, includePrivateParameters: false);

        // Act
        var result = client.ImportKey(new ImportKeyOptions("imported-ec-key", jwk));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.Name, Is.EqualTo("imported-ec-key"));
            Assert.That(result.Value.KeyType, Is.EqualTo(KeyType.Ec));
            Assert.That(result.Value.Key.CurveName, Is.EqualTo(KeyCurveName.P256));
            Assert.That(result.Value.Key.X, Is.Not.Null);
            Assert.That(result.Value.Key.Y, Is.Not.Null);
        });
    }

    [Test]
    public void KeyVaultKeyTests_ImportKeyTwice_ShouldCreateNewVersions()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        using var rsa1 = RSA.Create(2048);
        using var rsa2 = RSA.Create(2048);

        // Act
        var v1 = client.ImportKey(new ImportKeyOptions("import-versioned", new JsonWebKey(rsa1, includePrivateParameters: false)));
        var v2 = client.ImportKey(new ImportKeyOptions("import-versioned", new JsonWebKey(rsa2, includePrivateParameters: false)));

        // Assert — each import creates a distinct version
        Assert.That(v1.Value.Id.ToString(), Is.Not.EqualTo(v2.Value.Id.ToString()));
    }

    [Test]
    public void KeyVaultKeyTests_ImportedKeyCanBeRetrievedByGet()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        using var rsa = RSA.Create(2048);
        client.ImportKey(new ImportKeyOptions("get-after-import", new JsonWebKey(rsa, includePrivateParameters: false)));

        // Act
        var result = client.GetKey("get-after-import");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.Name, Is.EqualTo("get-after-import"));
            Assert.That(result.Value.KeyType, Is.EqualTo(KeyType.Rsa));
        });
    }

    [Test]
    public void KeyVaultKeyTests_GetKey_LatestVersion_ReturnsNewestBundle()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Create two versions
        var v1 = client.CreateRsaKey(new CreateRsaKeyOptions("get-latest-key"));
        var v2 = client.CreateRsaKey(new CreateRsaKeyOptions("get-latest-key"));

        // Act — no version → latest
        var result = client.GetKey("get-latest-key");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.Id.ToString(), Is.EqualTo(v2.Value.Id.ToString()));
        });
    }

    [Test]
    public void KeyVaultKeyTests_GetKey_SpecificVersion_ReturnsMatchingBundle()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        var v1 = client.CreateRsaKey(new CreateRsaKeyOptions("get-version-key"));
        client.CreateRsaKey(new CreateRsaKeyOptions("get-version-key")); // create a second version

        // Extract the version segment from the v1 kid (last path segment)
        var v1Version = v1.Value.Id.ToString().Split('/').Last();

        // Act
        var result = client.GetKey("get-version-key", v1Version);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.Id.ToString(), Is.EqualTo(v1.Value.Id.ToString()));
        });
    }

    [Test]
    public void KeyVaultKeyTests_GetKey_NonExistentKey_ThrowsException()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act & Assert
        Assert.Throws<RequestFailedException>(() => client.GetKey("key-that-does-not-exist"));
    }

    [Test]
    public void KeyVaultKeyTests_GetKey_IdContainsVaultNameAndKeyName()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("id-verify-key"));

        // Act
        var result = client.GetKey("id-verify-key");

        // Assert
        var kid = result.Value.Id.ToString();
        Assert.Multiple(() =>
        {
            Assert.That(kid, Does.Contain(TestKeyVaultName));
            Assert.That(kid, Does.Contain("keys"));
            Assert.That(kid, Does.Contain("id-verify-key"));
        });
    }

    [Test]
    public void KeyVaultKeyTests_GetKeyVersions_ReturnsAllVersions()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        client.CreateRsaKey(new CreateRsaKeyOptions("versioned-list-key"));
        client.CreateRsaKey(new CreateRsaKeyOptions("versioned-list-key"));
        client.CreateRsaKey(new CreateRsaKeyOptions("versioned-list-key"));

        // Act
        var versions = client.GetPropertiesOfKeyVersions("versioned-list-key").ToList();

        // Assert
        Assert.That(versions, Has.Count.EqualTo(3));
    }

    [Test]
    public void KeyVaultKeyTests_GetKeyVersions_EachVersionHasDistinctId()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        client.CreateRsaKey(new CreateRsaKeyOptions("distinct-versions-key"));
        client.CreateRsaKey(new CreateRsaKeyOptions("distinct-versions-key"));

        // Act
        var versions = client.GetPropertiesOfKeyVersions("distinct-versions-key").ToList();

        // Assert
        var ids = versions.Select(v => v.Id.ToString()).ToList();
        Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
    }

    [Test]
    public void KeyVaultKeyTests_GetKeyVersions_VersionsHaveCorrectKeyName()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        client.CreateRsaKey(new CreateRsaKeyOptions("name-check-versions-key"));
        client.CreateRsaKey(new CreateRsaKeyOptions("name-check-versions-key"));

        // Act
        var versions = client.GetPropertiesOfKeyVersions("name-check-versions-key").ToList();

        // Assert
        Assert.That(versions, Has.All.Matches<KeyProperties>(v => v.Name == "name-check-versions-key"));
    }

    [Test]
    public void KeyVaultKeyTests_GetKeyVersions_NonExistentKey_ThrowsException()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act & Assert — enumerating a non-existent key should throw
        Assert.Throws<RequestFailedException>(
            () => client.GetPropertiesOfKeyVersions("nonexistent-versions-key").ToList());
    }
}
