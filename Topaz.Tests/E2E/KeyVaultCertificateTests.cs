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

        // Act — delete is synchronous; WaitForCompletion would poll GET /deletedcertificates/{name}
        // which is out of scope, so just fire the DELETE request without waiting for the LRO poll.
        client.StartDeleteCertificate("delete-cert");

        // Assert — certificate no longer in active list
        var active = client.GetPropertiesOfCertificates().ToList();
        Assert.That(active.Any(c => c.Name == "delete-cert"), Is.False);
    }
}
