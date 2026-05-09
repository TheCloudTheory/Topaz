using Topaz.CLI;
using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.Security.KeyVault.Certificates;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class KeyVaultCertificateTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("B9D44E7A-2C31-4F58-A1EC-7308B52D6EF3");

    private const string SubscriptionName = "sub-kv-certs-test";
    private const string ResourceGroupName = "test-kv-certs";
    private const string TestKeyVaultName = "testkvcerts";

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

    private CertificateClient CreateCertificateClient()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        return new CertificateClient(
            vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(TestKeyVaultName),
            credential: credential,
            new CertificateClientOptions { DisableChallengeResourceVerification = true });
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
    public void Certificate_Create_ShouldReturnCreatedCertificate()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        var policy = new CertificatePolicy("Self", "CN=test-cert");

        // Act
        var createOp = client.StartCreateCertificate("create-cert", policy);
        var result = createOp.WaitForCompletion();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.Name, Is.EqualTo("create-cert"));
            Assert.That(result.Value.Id, Is.Not.Null);
        });
    }

    [Test]
    public void Certificate_Import_ShouldReturnImportedCertificate()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();

        // Generate a self-signed cert in memory for import
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=import-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
        var pfxBytes = cert.Export(X509ContentType.Pfx);

        // Act
        var imported = client.ImportCertificate(new ImportCertificateOptions("import-cert", pfxBytes));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(imported.Value, Is.Not.Null);
            Assert.That(imported.Value.Name, Is.EqualTo("import-cert"));
            Assert.That(imported.Value.Cer, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public void Certificate_Get_ShouldReturnByNameAndVersion()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        var policy = new CertificatePolicy("Self", "CN=get-test");
        var createOp = client.StartCreateCertificate("get-cert", policy);
        var created = createOp.WaitForCompletion().Value;

        // Act — get by name only
        var byName = client.GetCertificate("get-cert");

        // Act — get by version
        var version = created.Properties.Version;
        var byVersion = client.GetCertificateVersion("get-cert", version);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(byName.Value, Is.Not.Null);
            Assert.That(byName.Value.Name, Is.EqualTo("get-cert"));
            Assert.That(byVersion.Value, Is.Not.Null);
            Assert.That(byVersion.Value.Properties.Version, Is.EqualTo(version));
        });
    }

    [Test]
    public void Certificate_GetCertificates_ShouldListAll()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        var policy = new CertificatePolicy("Self", "CN=list-test");

        client.StartCreateCertificate("list-cert-one", policy).WaitForCompletion();
        client.StartCreateCertificate("list-cert-two", policy).WaitForCompletion();

        // Act
        var certs = client.GetPropertiesOfCertificates().ToList();

        // Assert
        Assert.That(certs.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(certs.Any(c => c.Name == "list-cert-one"), Is.True);
        Assert.That(certs.Any(c => c.Name == "list-cert-two"), Is.True);
    }

    [Test]
    public void Certificate_GetVersions_ShouldReturnAllVersions()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        var policy = new CertificatePolicy("Self", "CN=versions-test");

        client.StartCreateCertificate("versioned-cert", policy).WaitForCompletion();
        client.StartCreateCertificate("versioned-cert", policy).WaitForCompletion();

        // Act
        var versions = client.GetPropertiesOfCertificateVersions("versioned-cert").ToList();

        // Assert
        Assert.That(versions, Has.Count.EqualTo(2));
    }

    [Test]
    public void Certificate_Update_ShouldReflectAttributeChange()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        var policy = new CertificatePolicy("Self", "CN=update-test");
        var createOp = client.StartCreateCertificate("update-cert", policy);
        createOp.WaitForCompletion();

        var fetched = client.GetCertificate("update-cert").Value;

        // Act
        fetched.Properties.Enabled = false;
        var updated = client.UpdateCertificateProperties(fetched.Properties);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updated.Value, Is.Not.Null);
            Assert.That(updated.Value.Properties.Enabled, Is.False);
        });
    }

    [Test]
    public void Certificate_Delete_ShouldRemoveFromActiveList()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        var policy = new CertificatePolicy("Self", "CN=delete-test");
        client.StartCreateCertificate("delete-cert", policy).WaitForCompletion();

        // Act — WaitForCompletion polls GET /deletedcertificates/{name}, now implemented.
        client.StartDeleteCertificate("delete-cert").WaitForCompletion();

        // Assert — certificate no longer in active list
        var active = client.GetPropertiesOfCertificates().ToList();
        Assert.That(active.Any(c => c.Name == "delete-cert"), Is.False);
    }

    [Test]
    public void Certificate_GetDeleted_ShouldReturnDeletedBundle()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        var policy = new CertificatePolicy("Self", "CN=get-deleted-test");
        client.StartCreateCertificate("get-deleted-cert", policy).WaitForCompletion();
        client.StartDeleteCertificate("get-deleted-cert").WaitForCompletion();

        // Act
        var deleted = client.GetDeletedCertificate("get-deleted-cert").Value;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deleted.Name, Is.EqualTo("get-deleted-cert"));
            Assert.That(deleted.RecoveryId.ToString(), Does.Contain("/deletedcertificates/get-deleted-cert"));
        });
    }

    [Test]
    public void Certificate_GetDeletedCertificates_ShouldListDeleted()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        var policy = new CertificatePolicy("Self", "CN=list-deleted-test");
        client.StartCreateCertificate("list-deleted-cert", policy).WaitForCompletion();
        client.StartDeleteCertificate("list-deleted-cert").WaitForCompletion();

        // Act
        var deleted = client.GetDeletedCertificates().ToList();

        // Assert
        Assert.That(deleted.Any(c => c.Name == "list-deleted-cert"), Is.True);
    }

    [Test]
    public void Certificate_RecoverDeleted_ShouldRestoreToActiveList()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        var policy = new CertificatePolicy("Self", "CN=recover-test");
        client.StartCreateCertificate("recover-cert", policy).WaitForCompletion();
        client.StartDeleteCertificate("recover-cert").WaitForCompletion();

        var activeBeforeRecover = client.GetPropertiesOfCertificates().ToList();
        Assert.That(activeBeforeRecover.Any(c => c.Name == "recover-cert"), Is.False);

        // Act
        client.StartRecoverDeletedCertificate("recover-cert").WaitForCompletion();

        // Assert
        var active = client.GetPropertiesOfCertificates().ToList();
        Assert.That(active.Any(c => c.Name == "recover-cert"), Is.True);
    }

    [Test]
    public void Certificate_PurgeDeleted_ShouldPermanentlyRemove()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        var policy = new CertificatePolicy("Self", "CN=purge-test");
        client.StartCreateCertificate("purge-cert", policy).WaitForCompletion();
        client.StartDeleteCertificate("purge-cert").WaitForCompletion();

        // Act
        client.PurgeDeletedCertificate("purge-cert");

        // Assert — certificate is no longer in the deleted list
        var deletedAfterPurge = client.GetDeletedCertificates().ToList();
        Assert.That(deletedAfterPurge.Any(c => c.Name == "purge-cert"), Is.False);
    }

    [Test]
    public void Certificate_Backup_ShouldReturnNonEmptyBlob()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        var policy = new CertificatePolicy("Self", "CN=backup-test");
        client.StartCreateCertificate("backup-cert", policy).WaitForCompletion();

        // Act
        var backupResult = client.BackupCertificate("backup-cert");

        // Assert
        Assert.That(backupResult.Value, Is.Not.Empty);
    }

    [Test]
    public void Certificate_Restore_ShouldRecreateFromBackup()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        var policy = new CertificatePolicy("Self", "CN=restore-test");
        client.StartCreateCertificate("restore-cert", policy).WaitForCompletion();

        // Back up
        var backupBlob = client.BackupCertificate("restore-cert").Value;
        Assert.That(backupBlob, Is.Not.Empty);

        // Delete (no wait)
        client.StartDeleteCertificate("restore-cert");
        var activeAfterDelete = client.GetPropertiesOfCertificates().ToList();
        Assert.That(activeAfterDelete.Any(c => c.Name == "restore-cert"), Is.False);

        // Restore
        var restored = client.RestoreCertificateBackup(backupBlob).Value;

        // Assert restored certificate is accessible
        Assert.Multiple(() =>
        {
            Assert.That(restored.Name, Is.EqualTo("restore-cert"));
            Assert.That(restored.Id.ToString(), Does.Contain("/certificates/restore-cert/"));
        });
    }

    [Test]
    public void CertificateContacts_Set_ShouldReturnContacts()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();

        // Act
        var contacts = client.SetContacts(new[]
        {
            new CertificateContact { Email = "admin@example.com", Name = "Admin", Phone = "555-0100" }
        });

        // Assert — Value is IList<CertificateContact>
        Assert.Multiple(() =>
        {
            Assert.That(contacts.Value, Is.Not.Null);
            Assert.That(contacts.Value, Has.Count.EqualTo(1));
            Assert.That(contacts.Value[0].Email, Is.EqualTo("admin@example.com"));
        });
    }

    [Test]
    public void CertificateContacts_Get_ShouldReturnStoredContacts()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        client.SetContacts(new[]
        {
            new CertificateContact { Email = "get@example.com", Name = "GetUser" }
        });

        // Act
        var contacts = client.GetContacts();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(contacts.Value, Is.Not.Null);
            Assert.That(contacts.Value, Has.Count.EqualTo(1));
            Assert.That(contacts.Value[0].Email, Is.EqualTo("get@example.com"));
        });
    }

    [Test]
    public void CertificateContacts_Delete_ShouldClearContacts()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        client.SetContacts(new[]
        {
            new CertificateContact { Email = "delete@example.com", Name = "DeleteUser" }
        });

        // Act
        var deleted = client.DeleteContacts();

        // Assert — delete returns the now-empty list
        Assert.That(deleted.Value, Is.Not.Null);
        Assert.That(deleted.Value, Is.Empty);
    }

    [Test]
    public void Issuer_Set_ShouldReturnIssuerBundle()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        var issuer = new CertificateIssuer("test-issuer", "Test");

        // Act
        var result = client.CreateIssuer(issuer);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.Name, Is.EqualTo("test-issuer"));
            Assert.That(result.Value.Provider, Is.EqualTo("Test"));
            Assert.That(result.Value.Id, Is.Not.Null);
        });
    }

    [Test]
    public void Issuer_Get_ShouldReturnIssuerBundle()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        client.CreateIssuer(new CertificateIssuer("get-issuer", "Test"));

        // Act
        var result = client.GetIssuer("get-issuer");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.Name, Is.EqualTo("get-issuer"));
            Assert.That(result.Value.Provider, Is.EqualTo("Test"));
        });
    }

    [Test]
    public void Issuer_Update_ShouldReturnUpdatedBundle()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        client.CreateIssuer(new CertificateIssuer("update-issuer", "Test"));

        var updated = new CertificateIssuer("update-issuer", "DigiCert");

        // Act
        var result = client.UpdateIssuer(updated);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.Provider, Is.EqualTo("DigiCert"));
        });
    }

    [Test]
    public void Issuer_Delete_ShouldReturnDeletedBundle()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        client.CreateIssuer(new CertificateIssuer("delete-issuer", "Test"));

        // Act
        var result = client.DeleteIssuer("delete-issuer");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.Name, Is.EqualTo("delete-issuer"));
        });
    }

    [Test]
    public void Issuer_List_ShouldReturnAllIssuers()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        client.CreateIssuer(new CertificateIssuer("list-issuer-a", "Test"));
        client.CreateIssuer(new CertificateIssuer("list-issuer-b", "Test"));

        // Act
        var issuers = client.GetPropertiesOfIssuers().ToList();

        // Assert
        Assert.That(issuers, Has.Count.GreaterThanOrEqualTo(2));
        Assert.That(issuers.Select(i => i.Name), Does.Contain("list-issuer-a"));
        Assert.That(issuers.Select(i => i.Name), Does.Contain("list-issuer-b"));
    }

    [Test]
    public void CertificateOperation_GetPending_ShouldReturnOperation()
    {
        // Arrange
        EnsureVault();
        var client = CreateCertificateClient();
        var policy = new CertificatePolicy("Self", "CN=pending-get-test");
        client.StartCreateCertificate("pending-get-cert", policy).WaitForCompletion();

        // Act — GET /certificates/{name}/pending
        var operation = client.GetCertificateOperation("pending-get-cert");
        operation.UpdateStatus();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(operation.Properties, Is.Not.Null);
            Assert.That(operation.Properties.Name, Is.EqualTo("pending-get-cert"));
            Assert.That(operation.Properties.Status, Is.EqualTo("completed"));
        });
    }

    [Test]
    public void CertificateOperation_UpdatePending_ShouldSetCancellationRequested()
    {
        // Arrange — create a cert so a .pending.json file exists
        EnsureVault();
        var client = CreateCertificateClient();
        var policy = new CertificatePolicy("Self", "CN=pending-update-test");
        client.StartCreateCertificate("pending-update-cert", policy).WaitForCompletion();

        // Act — PATCH /certificates/{name}/pending with cancellation_requested=true
        // Cancel() issues the PATCH and updates Properties in place from the response.
        var operation = client.GetCertificateOperation("pending-update-cert");
        operation.Cancel();

        // Assert
        Assert.That(operation.Properties.CancellationRequested, Is.True);
    }

    [Test]
    public void CertificateOperation_DeletePending_ShouldRemoveOperation()
    {
        // Arrange — create a cert so a .pending.json file exists
        EnsureVault();
        var client = CreateCertificateClient();
        var policy = new CertificatePolicy("Self", "CN=pending-delete-test");
        client.StartCreateCertificate("pending-delete-cert", policy).WaitForCompletion();

        // Verify the operation exists first
        var operation = client.GetCertificateOperation("pending-delete-cert");
        operation.UpdateStatus();
        Assert.That(operation.Properties, Is.Not.Null);

        // Act — DELETE /certificates/{name}/pending; Delete() returns void on success
        Assert.DoesNotThrow(() => operation.Delete());
    }

    [Test]
    public void Certificate_Merge_ShouldCompleteFromExternalChain()
    {
        // Arrange — create a cert with a Self issuer so a .pending.json exists.
        // Then simulate an external CA by generating a separate self-signed leaf and
        // merging its DER bytes as the "CA response".
        EnsureVault();
        var client = CreateCertificateClient();
        var policy = new CertificatePolicy("Self", "CN=merge-test");
        client.StartCreateCertificate("merge-cert", policy).WaitForCompletion();

        // Build the "externally-signed" leaf certificate to be merged
        using var rsa = RSA.Create(2048);
        var certReq = new CertificateRequest("CN=merge-test-ext", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var externalCert = certReq.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
        var derBytes = externalCert.RawData;

        // Act — POST /certificates/{name}/pending/merge
        var mergeOptions = new MergeCertificateOptions("merge-cert", new[] { derBytes });
        var merged = client.MergeCertificate(mergeOptions);

        // Assert — the returned bundle represents the merged certificate
        Assert.Multiple(() =>
        {
            Assert.That(merged.Value, Is.Not.Null);
            Assert.That(merged.Value.Name, Is.EqualTo("merge-cert"));
            Assert.That(merged.Value.Cer, Is.Not.Null.And.Not.Empty);
            Assert.That(merged.Value.Id.ToString(), Does.Contain("/certificates/merge-cert/"));
        });
    }
}
