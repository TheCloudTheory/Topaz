using Topaz.CLI;
using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using System.Security.Cryptography;
using System.Text;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

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

    [Test]
    public void KeyVaultKeyTests_GetKeys_EmptyVault_ReturnsEmptyList()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act
        var keys = client.GetPropertiesOfKeys().ToList();

        // Assert
        Assert.That(keys, Is.Empty);
    }

    [Test]
    public void KeyVaultKeyTests_GetKeys_ReturnsAllKeys()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        client.CreateRsaKey(new CreateRsaKeyOptions("list-key-one"));
        client.CreateRsaKey(new CreateRsaKeyOptions("list-key-two"));
        client.CreateEcKey(new CreateEcKeyOptions("list-key-three"));

        // Act
        var keys = client.GetPropertiesOfKeys().ToList();

        // Assert
        Assert.That(keys, Has.Count.EqualTo(3));
    }

    [Test]
    public void KeyVaultKeyTests_GetKeys_EachKeyHasCorrectName()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        client.CreateRsaKey(new CreateRsaKeyOptions("named-key-alpha"));
        client.CreateRsaKey(new CreateRsaKeyOptions("named-key-beta"));

        // Act
        var keys = client.GetPropertiesOfKeys().Select(k => k.Name).ToList();

        // Assert
        Assert.That(keys, Is.EquivalentTo(new[] { "named-key-alpha", "named-key-beta" }));
    }

    [Test]
    public void KeyVaultKeyTests_GetKeys_MultipleVersionsOfSameKey_ReturnsSingleEntry()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        client.CreateRsaKey(new CreateRsaKeyOptions("multi-version-list-key"));
        client.CreateRsaKey(new CreateRsaKeyOptions("multi-version-list-key"));
        client.CreateRsaKey(new CreateRsaKeyOptions("multi-version-list-key"));

        // Act
        var keys = client.GetPropertiesOfKeys().ToList();

        // Assert — three versions of the same key → only one entry in the list
        Assert.That(keys, Has.Count.EqualTo(1));
        Assert.That(keys[0].Name, Is.EqualTo("multi-version-list-key"));
    }

    [Test]
    public void KeyVaultKeyTests_GetKeys_KeyHasValidId()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("id-list-key"));

        // Act
        var keys = client.GetPropertiesOfKeys().ToList();

        // Assert
        Assert.That(keys, Has.Count.EqualTo(1));
        Assert.That(keys[0].Id.ToString(), Does.Contain(TestKeyVaultName));
        Assert.That(keys[0].Id.ToString(), Does.Contain("keys"));
        Assert.That(keys[0].Id.ToString(), Does.Contain("id-list-key"));
    }

    [Test]
    public void KeyVaultKeyTests_UpdateKey_DisableKey_ShouldReflectInAttributes()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        var created = client.CreateRsaKey(new CreateRsaKeyOptions("update-disable-key"));
        var version = created.Value.Id.ToString().Split('/').Last();

        // Act
        var props = created.Value.Properties;
        props.Enabled = false;
        var updated = client.UpdateKeyProperties(props);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updated.Value, Is.Not.Null);
            Assert.That(updated.Value.Properties.Enabled, Is.False);
            Assert.That(updated.Value.Id.ToString(), Does.Contain(version));
        });
    }

    [Test]
    public void KeyVaultKeyTests_UpdateKey_EnableKey_ShouldReflectInAttributes()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        var created = client.CreateRsaKey(new CreateRsaKeyOptions("update-enable-key")
        {
            Enabled = false
        });

        // Act
        var props = created.Value.Properties;
        props.Enabled = true;
        var updated = client.UpdateKeyProperties(props);

        // Assert
        Assert.That(updated.Value.Properties.Enabled, Is.True);
    }

    [Test]
    public void KeyVaultKeyTests_UpdateKey_UpdatedTimestampChanges()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        var created = client.CreateRsaKey(new CreateRsaKeyOptions("update-timestamp-key"));
        var originalUpdated = created.Value.Properties.UpdatedOn;

        // Ensure time advances
        System.Threading.Thread.Sleep(1100);

        // Act
        var props = created.Value.Properties;
        props.Enabled = true;
        var updated = client.UpdateKeyProperties(props);

        // Assert
        Assert.That(updated.Value.Properties.UpdatedOn, Is.GreaterThan(originalUpdated));
    }

    [Test]
    public void KeyVaultKeyTests_UpdateKey_NonExistentKey_ThrowsException()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        var fakeId = new Uri($"https://{TestKeyVaultName}.vault.azure.net/keys/nonexistent-key/{Guid.NewGuid():N}");

        // Act & Assert
        var fakeProps = new KeyProperties(fakeId);
        fakeProps.Enabled = false;
        Assert.Throws<RequestFailedException>(() => client.UpdateKeyProperties(fakeProps));
    }

    // ── Backup Key ───────────────────────────────────────────────────────────

    [Test]
    public void KeyVaultKeyTests_BackupKey_ReturnsNonNullBytes()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("backup-rsa-key"));

        // Act
        var result = client.BackupKey("backup-rsa-key");

        // Assert
        Assert.That(result.Value, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void KeyVaultKeyTests_BackupKey_NonExistentKey_ThrowsException()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act & Assert
        Assert.Throws<RequestFailedException>(() => client.BackupKey("key-that-does-not-exist"));
    }

    [Test]
    public void KeyVaultKeyTests_BackupKey_MultipleVersions_BackupSucceeds()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("multi-version-backup-key"));
        client.CreateRsaKey(new CreateRsaKeyOptions("multi-version-backup-key"));

        // Act
        var result = client.BackupKey("multi-version-backup-key");

        // Assert
        Assert.That(result.Value, Is.Not.Null.And.Not.Empty);
    }

    // ── Restore Key ──────────────────────────────────────────────────────────

    [Test]
    public void KeyVaultKeyTests_RestoreKey_RoundTrip_RestoredKeyIsRetrievable()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        const string keyName = "restore-rsa-key";
        client.CreateRsaKey(new CreateRsaKeyOptions(keyName));
        var backup = client.BackupKey(keyName).Value;

        // Act
        var restored = client.RestoreKeyBackup(backup);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(restored.Value, Is.Not.Null);
            Assert.That(restored.Value.Name, Is.EqualTo(keyName));
        });

        // Confirm retrievable
        var fetched = client.GetKey(keyName);
        Assert.That(fetched.Value.Name, Is.EqualTo(keyName));
    }

    [Test]
    public void KeyVaultKeyTests_RestoreKey_InvalidBlob_ThrowsRequestFailedException()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act & Assert
        Assert.Throws<RequestFailedException>(() => client.RestoreKeyBackup(Encoding.UTF8.GetBytes("not-a-valid-blob")));
    }

    [Test]
    public void KeyVaultKeyTests_RestoreKey_MultipleVersions_AllVersionsRestored()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        const string keyName = "restore-multi-version-key";
        client.CreateRsaKey(new CreateRsaKeyOptions(keyName));
        client.CreateRsaKey(new CreateRsaKeyOptions(keyName));
        var backup = client.BackupKey(keyName).Value;

        // Act
        var restored = client.RestoreKeyBackup(backup);

        // Assert
        Assert.That(restored.Value.Name, Is.EqualTo(keyName));

        var versions = client.GetPropertiesOfKeyVersions(keyName).ToList();
        Assert.That(versions.Count, Is.GreaterThanOrEqualTo(2));
    }

    // ── Delete Key ────────────────────────────────────────────────────────────

    [Test]
    public void KeyVaultKeyTests_DeleteKey_ExistingKey_ReturnsDeletedKeyBundle()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("delete-me-key"));

        // Act
        var deleted = client.StartDeleteKey("delete-me-key");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deleted.Value, Is.Not.Null);
            Assert.That(deleted.Value.Name, Is.EqualTo("delete-me-key"));
            Assert.That(deleted.Value.RecoveryId.ToString(), Does.Contain("deletedkeys/delete-me-key"));
        });
    }

    [Test]
    public void KeyVaultKeyTests_DeleteKey_KeyNoLongerListedAfterDeletion()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("delete-list-key"));

        // Act
        client.StartDeleteKey("delete-list-key");
        var keys = client.GetPropertiesOfKeys().ToList();

        // Assert
        Assert.That(keys.Any(k => k.Name == "delete-list-key"), Is.False);
    }

    [Test]
    public void KeyVaultKeyTests_DeleteKey_NonExistentKey_ThrowsException()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act & Assert
        Assert.Throws<RequestFailedException>(() => client.StartDeleteKey("nonexistent-delete-key"));
    }

    [Test]
    public void KeyVaultKeyTests_DeleteKey_DeletedKeyHasRecoveryId()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("delete-recovery-key"));

        // Act
        var deleted = client.StartDeleteKey("delete-recovery-key");

        // Assert
        Assert.That(deleted.Value.RecoveryId, Is.Not.Null.And.Not.EqualTo(new Uri("about:blank")));
    }

    // ── Get Deleted Key ───────────────────────────────────────────────────────

    [Test]
    public void KeyVaultKeyTests_GetDeletedKey_AfterDeletion_ReturnsDeletedKeyProperties()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("get-deleted-key"));
        client.StartDeleteKey("get-deleted-key");

        // Act
        var deleted = client.GetDeletedKey("get-deleted-key");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deleted.Value, Is.Not.Null);
            Assert.That(deleted.Value.Name, Is.EqualTo("get-deleted-key"));
            Assert.That(deleted.Value.RecoveryId.ToString(), Does.Contain("deletedkeys/get-deleted-key"));
        });
    }

    [Test]
    public void KeyVaultKeyTests_GetDeletedKey_NonExistentKey_ThrowsRequestFailedException()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act & Assert
        Assert.Throws<RequestFailedException>(() => client.GetDeletedKey("key-never-deleted"));
    }

    // ── Get Deleted Keys ──────────────────────────────────────────────────────

    [Test]
    public void KeyVaultKeyTests_GetDeletedKeys_EmptyVault_ReturnsEmptyList()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act
        var deletedKeys = client.GetDeletedKeys().ToList();

        // Assert
        Assert.That(deletedKeys, Is.Empty);
    }

    [Test]
    public void KeyVaultKeyTests_GetDeletedKeys_AfterDeletion_ContainsDeletedKey()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("list-deleted-key"));
        client.StartDeleteKey("list-deleted-key");

        // Act
        var deletedKeys = client.GetDeletedKeys().ToList();

        // Assert
        Assert.That(deletedKeys, Has.Count.EqualTo(1));
        Assert.That(deletedKeys[0].Name, Is.EqualTo("list-deleted-key"));
    }

    [Test]
    public void KeyVaultKeyTests_GetDeletedKeys_MultipleDeletedKeys_ReturnsAll()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("multi-deleted-one"));
        client.CreateRsaKey(new CreateRsaKeyOptions("multi-deleted-two"));
        client.StartDeleteKey("multi-deleted-one");
        client.StartDeleteKey("multi-deleted-two");

        // Act
        var deletedKeys = client.GetDeletedKeys().Select(k => k.Name).ToList();

        // Assert
        Assert.That(deletedKeys, Is.EquivalentTo(new[] { "multi-deleted-one", "multi-deleted-two" }));
    }

    [Test]
    public void KeyVaultKeyTests_GetDeletedKeys_DeletedKeyHasRecoveryId()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("recovery-id-deleted-key"));
        client.StartDeleteKey("recovery-id-deleted-key");

        // Act
        var deletedKeys = client.GetDeletedKeys().ToList();

        // Assert
        Assert.That(deletedKeys, Has.Count.EqualTo(1));
        Assert.That(deletedKeys[0].RecoveryId.ToString(), Does.Contain("deletedkeys/recovery-id-deleted-key"));
    }

    [Test]
    public void KeyVaultKeyTests_GetDeletedKeys_ActiveKeyNotIncluded()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("active-key-not-deleted"));
        client.CreateRsaKey(new CreateRsaKeyOptions("deleted-key-in-list"));
        client.StartDeleteKey("deleted-key-in-list");

        // Act
        var deletedKeys = client.GetDeletedKeys().Select(k => k.Name).ToList();

        // Assert
        Assert.That(deletedKeys, Does.Contain("deleted-key-in-list"));
        Assert.That(deletedKeys, Does.Not.Contain("active-key-not-deleted"));
    }

    // ── Recover Deleted Key ───────────────────────────────────────────────────

    [Test]
    public void KeyVaultKeyTests_RecoverDeletedKey_AfterDeletion_KeyAccessibleAgain()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("recover-key"));
        var deleteOperation = client.StartDeleteKey("recover-key");
        deleteOperation.WaitForCompletion();

        // Act
        var recoverOperation = client.StartRecoverDeletedKey("recover-key");
        recoverOperation.WaitForCompletion();
        var recovered = client.GetKey("recover-key");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(recovered.Value, Is.Not.Null);
            Assert.That(recovered.Value.Name, Is.EqualTo("recover-key"));
        });
    }

    [Test]
    public void KeyVaultKeyTests_RecoverDeletedKey_AfterRecovery_KeyRemovedFromDeletedList()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("recover-list-cleanup-key"));
        var deleteOperation = client.StartDeleteKey("recover-list-cleanup-key");
        deleteOperation.WaitForCompletion();

        // Act
        var recoverOperation = client.StartRecoverDeletedKey("recover-list-cleanup-key");
        recoverOperation.WaitForCompletion();
        var deletedKeys = client.GetDeletedKeys().Select(key => key.Name).ToList();

        // Assert
        Assert.That(deletedKeys, Does.Not.Contain("recover-list-cleanup-key"));
    }

    [Test]
    public void KeyVaultKeyTests_RecoverDeletedKey_MultipleVersions_AllVersionsRestored()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        const string keyName = "recover-versioned-key";
        client.CreateRsaKey(new CreateRsaKeyOptions(keyName));
        client.CreateRsaKey(new CreateRsaKeyOptions(keyName));
        var deleteOperation = client.StartDeleteKey(keyName);
        deleteOperation.WaitForCompletion();

        // Act
        var recoverOperation = client.StartRecoverDeletedKey(keyName);
        recoverOperation.WaitForCompletion();
        var versions = client.GetPropertiesOfKeyVersions(keyName).ToList();

        // Assert
        Assert.That(versions.Count, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void KeyVaultKeyTests_RecoverDeletedKey_NonExistentKey_ThrowsRequestFailedException()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act & Assert
        Assert.Throws<RequestFailedException>(() => client.StartRecoverDeletedKey("recover-never-deleted"));
    }

    [Test]
    public void KeyVaultKeyTests_PurgeDeletedKey_RemovesKeyFromDeletedList()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("purge-key"));
        var deleteOperation = client.StartDeleteKey("purge-key");
        deleteOperation.WaitForCompletion();

        // Act
        client.PurgeDeletedKey("purge-key");

        // Assert — the key must not appear in the deleted list after purge
        var deletedKeys = client.GetDeletedKeys().Select(k => k.Name).ToList();
        Assert.That(deletedKeys, Does.Not.Contain("purge-key"));
    }

    [Test]
    public void KeyVaultKeyTests_PurgeDeletedKey_NonExistentKey_ThrowsRequestFailedException()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act & Assert
        Assert.Throws<RequestFailedException>(() => client.PurgeDeletedKey("never-deleted-key"));
    }

    [Test]
    public void KeyVaultKeyTests_PurgeDeletedKey_PurgedKeyCannotBeRecovered()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("purge-no-recover-key"));
        var deleteOperation = client.StartDeleteKey("purge-no-recover-key");
        deleteOperation.WaitForCompletion();
        client.PurgeDeletedKey("purge-no-recover-key");

        // Act & Assert — recovering a purged key must fail
        Assert.Throws<RequestFailedException>(() => client.StartRecoverDeletedKey("purge-no-recover-key"));
    }

    [Test]
    public void KeyVaultKeyTests_RotateKey_CreatesNewVersion()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("rotate-rsa-key"));
        var original = client.GetKey("rotate-rsa-key");

        // Act
        var rotated = client.RotateKey("rotate-rsa-key");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(rotated.Value, Is.Not.Null);
            Assert.That(rotated.Value.Name, Is.EqualTo("rotate-rsa-key"));
            Assert.That(rotated.Value.Id, Is.Not.EqualTo(original.Value.Id),
                "Rotated key must have a new version ID");
        });
    }

    [Test]
    public void KeyVaultKeyTests_RotateKey_IncreasesVersionCount()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("rotate-versioned-key"));
        var versionsBefore = client.GetPropertiesOfKeyVersions("rotate-versioned-key").Count();

        // Act
        client.RotateKey("rotate-versioned-key");
        var versionsAfter = client.GetPropertiesOfKeyVersions("rotate-versioned-key").Count();

        // Assert
        Assert.That(versionsAfter, Is.EqualTo(versionsBefore + 1));
    }

    [Test]
    public void KeyVaultKeyTests_RotateKey_PreservesKeyType()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateEcKey(new CreateEcKeyOptions("rotate-ec-key") { CurveName = KeyCurveName.P384 });

        // Act
        var rotated = client.RotateKey("rotate-ec-key");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(rotated.Value.KeyType, Is.EqualTo(KeyType.Ec));
            Assert.That(rotated.Value.Key.CurveName, Is.EqualTo(KeyCurveName.P384));
        });
    }

    [Test]
    public void KeyVaultKeyTests_RotateKey_NonExistentKey_ThrowsRequestFailedException()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act & Assert
        Assert.Throws<RequestFailedException>(() => client.RotateKey("rotate-nonexistent-key"));
    }

    [Test]
    public void KeyVaultKeyTests_GetKeyRotationPolicy_ExistingKey_ReturnsPolicy()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("rotation-policy-key"));

        // Act
        var policy = client.GetKeyRotationPolicy("rotation-policy-key");

        // Assert
        Assert.That(policy.Value, Is.Not.Null);
    }

    [Test]
    public void KeyVaultKeyTests_GetKeyRotationPolicy_DefaultPolicy_HasEmptyLifetimeActions()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("rotation-policy-default-key"));

        // Act
        var policy = client.GetKeyRotationPolicy("rotation-policy-default-key");

        // Assert
        Assert.That(policy.Value.LifetimeActions, Is.Empty);
    }

    [Test]
    public void KeyVaultKeyTests_GetKeyRotationPolicy_NonExistentKey_ThrowsRequestFailedException()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act & Assert
        Assert.Throws<RequestFailedException>(() => client.GetKeyRotationPolicy("rotation-policy-nonexistent"));
    }

    [Test]
    public void KeyVaultKeyTests_UpdateKeyRotationPolicy_SetsExpiryTime_ReturnsUpdatedPolicy()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("update-rp-expiry-key"));

        var newPolicy = new KeyRotationPolicy
        {
            ExpiresIn = "P2Y"
        };

        // Act
        var result = client.UpdateKeyRotationPolicy("update-rp-expiry-key", newPolicy);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.ExpiresIn, Is.EqualTo("P2Y"));
        });
    }

    [Test]
    public void KeyVaultKeyTests_UpdateKeyRotationPolicy_WithLifetimeActions_StoresActions()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("update-rp-actions-key"));

        var newPolicy = new KeyRotationPolicy
        {
            LifetimeActions =
            {
                new KeyRotationLifetimeAction(KeyRotationPolicyAction.Rotate)
                {
                    TimeBeforeExpiry = "P30D"
                }
            }
        };

        // Act
        var result = client.UpdateKeyRotationPolicy("update-rp-actions-key", newPolicy);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.LifetimeActions, Has.Count.EqualTo(1));
            Assert.That(result.Value.LifetimeActions[0].Action, Is.EqualTo(KeyRotationPolicyAction.Rotate));
        });
    }

    [Test]
    public void KeyVaultKeyTests_UpdateKeyRotationPolicy_PersistsAcrossGet()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("update-rp-persist-key"));

        var newPolicy = new KeyRotationPolicy
        {
            ExpiresIn = "P1Y"
        };

        // Act
        client.UpdateKeyRotationPolicy("update-rp-persist-key", newPolicy);
        var retrieved = client.GetKeyRotationPolicy("update-rp-persist-key");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(retrieved.Value, Is.Not.Null);
            Assert.That(retrieved.Value.ExpiresIn, Is.EqualTo("P1Y"));
        });
    }

    [Test]
    public void KeyVaultKeyTests_UpdateKeyRotationPolicy_PreservesCreatedTimestamp()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();
        client.CreateRsaKey(new CreateRsaKeyOptions("update-rp-created-key"));

        var firstPolicy = new KeyRotationPolicy { ExpiresIn = "P1Y" };
        var firstResult = client.UpdateKeyRotationPolicy("update-rp-created-key", firstPolicy);
        var createdAfterFirst = firstResult.Value.CreatedOn;

        // Act — second update
        var secondPolicy = new KeyRotationPolicy { ExpiresIn = "P2Y" };
        var secondResult = client.UpdateKeyRotationPolicy("update-rp-created-key", secondPolicy);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(secondResult.Value.CreatedOn, Is.EqualTo(createdAfterFirst));
            Assert.That(secondResult.Value.UpdatedOn, Is.GreaterThanOrEqualTo(createdAfterFirst));
        });
    }

    [Test]
    public void KeyVaultKeyTests_UpdateKeyRotationPolicy_NonExistentKey_ThrowsRequestFailedException()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        var policy = new KeyRotationPolicy { ExpiresIn = "P1Y" };

        // Act & Assert
        Assert.Throws<RequestFailedException>(() => client.UpdateKeyRotationPolicy("update-rp-nonexistent-key", policy));
    }

    [Test]
    public void KeyVaultKeyTests_GetRandomBytes_ReturnsCorrectByteCount()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act
        var result = client.GetRandomBytes(32);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.Length, Is.EqualTo(32));
        });
    }

    [Test]
    public void KeyVaultKeyTests_GetRandomBytes_ReturnsDistinctResultsOnConsecutiveCalls()
    {
        // Arrange
        EnsureVault();
        var client = CreateKeyClient();

        // Act
        var r1 = client.GetRandomBytes(16);
        var r2 = client.GetRandomBytes(16);

        // Assert — it is astronomically unlikely for two 128-bit random values to be equal
        Assert.That(r1.Value.SequenceEqual(r2.Value), Is.False);
    }

    private CryptographyClient CreateCryptoClient(KeyVaultKey key)
    {
        // key.Id lacks the Topaz port (kid is stored without port number).
        // Build the key URI explicitly using the vault endpoint with port so the
        // CryptographyClient connects to port 8898 instead of the default 443.
        var version = key.Id.ToString().Split('/').Last();
        var keyUri = new Uri(
            $"https://{GlobalSettings.GetKeyVaultHost(TestKeyVaultName)}:{GlobalSettings.DefaultKeyVaultPort}" +
            $"/keys/{key.Name}/{version}");
        return new CryptographyClient(
            keyUri,
            new AzureLocalCredential(Globals.GlobalAdminId),
            new CryptographyClientOptions { DisableChallengeResourceVerification = true });
    }

    [Test]
    public void KeyVaultKeyTests_Encrypt_RsaOaep256_ShouldReturnCiphertext()
    {
        // Arrange
        EnsureVault();
        var keyClient = CreateKeyClient();
        var key = keyClient.CreateRsaKey(new CreateRsaKeyOptions("encrypt-test-key"));
        var cryptoClient = CreateCryptoClient(key.Value);
        var plaintext = Encoding.UTF8.GetBytes("hello topaz");

        // Act
        var result = cryptoClient.Encrypt(EncryptionAlgorithm.RsaOaep256, plaintext);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Ciphertext, Is.Not.Null);
            Assert.That(result.Ciphertext.Length, Is.GreaterThan(0));
            Assert.That(result.KeyId, Is.Not.Null.And.Contains("encrypt-test-key"));
        });
    }

    [Test]
    public void KeyVaultKeyTests_Encrypt_Rsa15_ShouldReturnCiphertext()
    {
        // Arrange
        EnsureVault();
        var keyClient = CreateKeyClient();
        var key = keyClient.CreateRsaKey(new CreateRsaKeyOptions("encrypt-rsa15-key"));
        var cryptoClient = CreateCryptoClient(key.Value);
        var plaintext = Encoding.UTF8.GetBytes("hello rsa15");

        // Act
        var result = cryptoClient.Encrypt(EncryptionAlgorithm.Rsa15, plaintext);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Ciphertext, Is.Not.Null);
            Assert.That(result.Ciphertext.Length, Is.GreaterThan(0));
        });
    }
}
