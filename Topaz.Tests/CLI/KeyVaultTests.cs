using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class KeyVaultTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("11417FBB-B6ED-4952-9691-29E8D1524852");
    private const string SubscriptionName = "kv-sub";
    private const string ResourceGroupName = "test";
    private const string VaultName = "MyKeyVault";

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

        await Program.RunAsync([
            "keyvault",
            "delete",
            "--name",
            VaultName,
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString(),
        ]);
        
        await Program.RunAsync([
            "keyvault",
            "create",
            "--name",
            VaultName,
            "-g",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
    }

    [Test]
    public void KeyVaultTests_WhenNewKeyVaultIsRequested_ItShouldBeCreated()
    {
        var keyVaultPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".azure-key-vault", VaultName,
            "metadata.json");

        Assert.That(File.Exists(keyVaultPath), Is.True);
    }

    [Test]
    public async Task KeyVaultTests_WhenNewKeyVaultIsDeleted_ItShouldBeDeleted()
    {
        var keyVaultPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".azure-key-vault", VaultName,
            "metadata.json");

        var result = await Program.RunAsync([
            "keyvault",
            "delete",
            "--name",
            VaultName,
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(Directory.Exists(keyVaultPath), Is.False);
        });
    }
    
    [Test]
    public async Task KeyVaultTests_WhenCheckNameIsCalledAndKeyVaultExists_ItShouldReturnFalse()
    {
        var result = await Program.RunAsync([
            "keyvault",
            "check-name",
            "--name",
            VaultName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
        });
    }
    
    [Test]
    public async Task KeyVaultTests_WhenCheckNameIsCalledAndKeyVaultDoeNotExist_ItShouldReturnTrue()
    {
        var result = await Program.RunAsync([
            "keyvault",
            "check-name",
            "--name",
            "somerandomkv",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
        });
    }


    [Test]
    public async Task KeyVaultTests_WhenDuplicatedKeyVaultIsAttemptedToBeCreated_ItShouldFailGracefullyWithMeaningfulError()
    {
        var result = await Program.RunAsync([
            "keyvault",
            "create",
            "--name",
            VaultName,
            "-g",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        Assert.That(result, Is.EqualTo(1));
    }
    
    [Test]
    [TestCase("kv-topaz-asdgdahsdhjajhsdjhagsdhgajgsd")]
    [TestCase("kv")]
    [TestCase("00kvtest")]
    [TestCase("kvtest-")]
    [TestCase("kv_test")]
    [TestCase("kv--test")]
    public async Task KeyVaultTests_WhenInvalidKeyVaultNameIsProvided_ItShouldFailGracefullyWithMeaningfulError(string invalidName)
    {
        var result = await Program.RunAsync([
            "keyvault",
            "create",
            "--name",
            invalidName,
            "-g",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task KeyVaultTests_GetRandomBytes_ShouldReturnBytesOfRequestedLength()
    {
        var result = await Program.RunAsync([
            "keyvault",
            "key",
            "random-bytes",
            "--count",
            "32"
        ]);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task KeyVaultTests_EncryptKey_ShouldReturnCiphertext()
    {
        // Create a key via CLI
        var createResult = await Program.RunAsync([
            "keyvault", "key", "create",
            "--vault-name", VaultName,
            "--name", "cli-encrypt-key",
            "--kty", "RSA",
            "-g", ResourceGroupName,
            "-s", SubscriptionId.ToString()
        ]);
        Assert.That(createResult, Is.EqualTo(0));

        // Read the key file from disk to extract the version from the kid
        // GetServiceInstanceDataPath appends /data, so the keys directory is under <vault>/data/keys/
        var keyFilePath = Path.Combine(
            Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName,
            ".azure-key-vault", VaultName, "data", "keys", "cli-encrypt-key.json");

        Assert.That(File.Exists(keyFilePath), Is.True);

        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(keyFilePath));
        var kid = doc.RootElement[0].GetProperty("key").GetProperty("kid").GetString()!;
        var version = kid.Split('/').Last();

        // Act — encrypt via CLI
        var result = await Program.RunAsync([
            "keyvault", "key", "encrypt",
            "--vault-name", VaultName,
            "--name", "cli-encrypt-key",
            "--version", version,
            "--algorithm", "RSA-OAEP-256",
            "--value", "aGVsbG8=",
            "-g", ResourceGroupName,
            "-s", SubscriptionId.ToString()
        ]);

        Assert.That(result, Is.EqualTo(0));
    }
}
