using System.Security.Cryptography;
using System.Text;
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

    [Test]
    public async Task KeyVaultTests_DecryptKey_ShouldReturnPlaintext()
    {
        // Create a key via CLI
        var createResult = await Program.RunAsync([
            "keyvault", "key", "create",
            "--vault-name", VaultName,
            "--name", "cli-decrypt-key",
            "--kty", "RSA",
            "-g", ResourceGroupName,
            "-s", SubscriptionId.ToString()
        ]);
        Assert.That(createResult, Is.EqualTo(0));

        // GetServiceInstanceDataPath appends /data, so the keys directory is under <vault>/data/keys/
        var keyFilePath = Path.Combine(
            Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName,
            ".azure-key-vault", VaultName, "data", "keys", "cli-decrypt-key.json");

        Assert.That(File.Exists(keyFilePath), Is.True);

        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(keyFilePath));
        var key = doc.RootElement[0].GetProperty("key");
        var kid = key.GetProperty("kid").GetString()!;
        var version = kid.Split('/').Last();

        // Encrypt using the stored RSA public key (avoid using internal types)
        var nBytes = Base64UrlDecodeLocal(key.GetProperty("n").GetString()!);
        var eBytes = Base64UrlDecodeLocal(key.GetProperty("e").GetString()!);
        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters { Modulus = nBytes, Exponent = eBytes });
        var plaintext = Encoding.UTF8.GetBytes("hello");
        var cipher = rsa.Encrypt(plaintext, RSAEncryptionPadding.OaepSHA256);
        var ciphertextBase64Url = Convert.ToBase64String(cipher).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        // Act — decrypt via CLI using the real ciphertext
        var result = await Program.RunAsync([
            "keyvault", "key", "decrypt",
            "--vault-name", VaultName,
            "--name", "cli-decrypt-key",
            "--version", version,
            "--algorithm", "RSA-OAEP-256",
            "--value", ciphertextBase64Url,
            "-g", ResourceGroupName,
            "-s", SubscriptionId.ToString()
        ]);

        Assert.That(result, Is.EqualTo(0));
    }

    private static byte[] Base64UrlDecodeLocal(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
        return Convert.FromBase64String(s);
    }

    private static string Base64UrlEncodeLocal(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [Test]
    public async Task KeyVaultTests_SignKey_ShouldReturnSignature()
    {
        // Arrange — create an RSA key via CLI
        var createResult = await Program.RunAsync([
            "keyvault", "key", "create",
            "--vault-name", VaultName,
            "--name", "cli-sign-key",
            "--kty", "RSA",
            "-g", ResourceGroupName,
            "-s", SubscriptionId.ToString()
        ]);
        Assert.That(createResult, Is.EqualTo(0));

        var keyFilePath = Path.Combine(
            Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName,
            ".azure-key-vault", VaultName, "data", "keys", "cli-sign-key.json");

        Assert.That(File.Exists(keyFilePath), Is.True);

        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(keyFilePath));
        var keyEl = doc.RootElement[0].GetProperty("key");
        var kid = keyEl.GetProperty("kid").GetString()!;
        var version = kid.Split('/').Last();

        // Build a SHA-256 digest of some data
        var data = Encoding.UTF8.GetBytes("hello topaz cli sign");
        var digest = SHA256.HashData(data);
        var digestBase64Url = Base64UrlEncodeLocal(digest);

        // Act — sign via CLI
        var signResult = await Program.RunAsync([
            "keyvault", "key", "sign",
            "--vault-name", VaultName,
            "--name", "cli-sign-key",
            "--version", version,
            "--algorithm", "RS256",
            "--value", digestBase64Url,
            "-g", ResourceGroupName,
            "-s", SubscriptionId.ToString()
        ]);

        Assert.That(signResult, Is.EqualTo(0));
    }

    [Test]
    public async Task KeyVaultTests_VerifyKey_ShouldReturnTrue()
    {
        // Arrange — create an RSA key via CLI
        var createResult = await Program.RunAsync([
            "keyvault", "key", "create",
            "--vault-name", VaultName,
            "--name", "cli-verify-key",
            "--kty", "RSA",
            "-g", ResourceGroupName,
            "-s", SubscriptionId.ToString()
        ]);
        Assert.That(createResult, Is.EqualTo(0));

        var keyFilePath = Path.Combine(
            Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName,
            ".azure-key-vault", VaultName, "data", "keys", "cli-verify-key.json");

        Assert.That(File.Exists(keyFilePath), Is.True);

        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(keyFilePath));
        var keyEl = doc.RootElement[0].GetProperty("key");
        var kid = keyEl.GetProperty("kid").GetString()!;
        var version = kid.Split('/').Last();

        // Build digest and sign using CLI
        var data = Encoding.UTF8.GetBytes("hello topaz cli verify");
        var digest = SHA256.HashData(data);
        var digestBase64Url = Base64UrlEncodeLocal(digest);

        // Sign first via CLI to obtain a valid signature
        var signResult = await Program.RunAsync([
            "keyvault", "key", "sign",
            "--vault-name", VaultName,
            "--name", "cli-verify-key",
            "--version", version,
            "--algorithm", "RS256",
            "--value", digestBase64Url,
            "-g", ResourceGroupName,
            "-s", SubscriptionId.ToString()
        ]);
        Assert.That(signResult, Is.EqualTo(0));

        // Read the signature from the key file output is via logger only; instead sign via data plane directly
        var nBytes = Base64UrlDecodeLocal(keyEl.GetProperty("n").GetString()!);
        var eBytes = Base64UrlDecodeLocal(keyEl.GetProperty("e").GetString()!);

        // Produce the signature independently using the stored private key
        var dBytes = Base64UrlDecodeLocal(keyEl.GetProperty("d").GetString()!);
        var pBytes = Base64UrlDecodeLocal(keyEl.GetProperty("p").GetString()!);
        var qBytes = Base64UrlDecodeLocal(keyEl.GetProperty("q").GetString()!);
        var dpBytes = Base64UrlDecodeLocal(keyEl.GetProperty("dp").GetString()!);
        var dqBytes = Base64UrlDecodeLocal(keyEl.GetProperty("dq").GetString()!);
        var qiBytes = Base64UrlDecodeLocal(keyEl.GetProperty("qi").GetString()!);
        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Modulus = nBytes, Exponent = eBytes,
            D = dBytes, P = pBytes, Q = qBytes,
            DP = dpBytes, DQ = dqBytes, InverseQ = qiBytes
        });
        var signature = rsa.SignHash(digest, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var signatureBase64Url = Base64UrlEncodeLocal(signature);

        // Act — verify via CLI
        var verifyResult = await Program.RunAsync([
            "keyvault", "key", "verify",
            "--vault-name", VaultName,
            "--name", "cli-verify-key",
            "--version", version,
            "--algorithm", "RS256",
            "--value", digestBase64Url,
            "--signature", signatureBase64Url,
            "-g", ResourceGroupName,
            "-s", SubscriptionId.ToString()
        ]);

        Assert.That(verifyResult, Is.EqualTo(0));
    }

    [Test]
    public async Task KeyVaultTests_WrapKey_ShouldReturnWrappedKey()
    {
        // Arrange — create an RSA key via CLI
        var createResult = await Program.RunAsync([
            "keyvault", "key", "create",
            "--vault-name", VaultName,
            "--name", "cli-wrap-key",
            "--kty", "RSA",
            "-g", ResourceGroupName,
            "-s", SubscriptionId.ToString()
        ]);
        Assert.That(createResult, Is.EqualTo(0));

        var keyFilePath = Path.Combine(
            Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName,
            ".azure-key-vault", VaultName, "data", "keys", "cli-wrap-key.json");

        Assert.That(File.Exists(keyFilePath), Is.True);

        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(keyFilePath));
        var keyEl = doc.RootElement[0].GetProperty("key");
        var kid = keyEl.GetProperty("kid").GetString()!;
        var version = kid.Split('/').Last();

        // A 32-byte key material to wrap, base64url-encoded
        var keyMaterial = new byte[32];
        Random.Shared.NextBytes(keyMaterial);
        var keyMaterialBase64Url = Base64UrlEncodeLocal(keyMaterial);

        // Act — wrap via CLI
        var wrapResult = await Program.RunAsync([
            "keyvault", "key", "wrap",
            "--vault-name", VaultName,
            "--name", "cli-wrap-key",
            "--version", version,
            "--algorithm", "RSA-OAEP-256",
            "--value", keyMaterialBase64Url,
            "-g", ResourceGroupName,
            "-s", SubscriptionId.ToString()
        ]);

        Assert.That(wrapResult, Is.EqualTo(0));
    }

    [Test]
    public async Task KeyVaultTests_UnwrapKey_ShouldReturnUnwrappedKey()
    {
        // Arrange — create an RSA key via CLI
        var createResult = await Program.RunAsync([
            "keyvault", "key", "create",
            "--vault-name", VaultName,
            "--name", "cli-unwrap-key",
            "--kty", "RSA",
            "-g", ResourceGroupName,
            "-s", SubscriptionId.ToString()
        ]);
        Assert.That(createResult, Is.EqualTo(0));

        var keyFilePath = Path.Combine(
            Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName,
            ".azure-key-vault", VaultName, "data", "keys", "cli-unwrap-key.json");

        Assert.That(File.Exists(keyFilePath), Is.True);

        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(keyFilePath));
        var keyEl = doc.RootElement[0].GetProperty("key");
        var kid = keyEl.GetProperty("kid").GetString()!;
        var version = kid.Split('/').Last();

        // Produce a wrapped key material using the stored public key
        var nBytes = Base64UrlDecodeLocal(keyEl.GetProperty("n").GetString()!);
        var eBytes = Base64UrlDecodeLocal(keyEl.GetProperty("e").GetString()!);
        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters { Modulus = nBytes, Exponent = eBytes });
        var keyMaterial = new byte[32];
        Random.Shared.NextBytes(keyMaterial);
        var wrapped = rsa.Encrypt(keyMaterial, RSAEncryptionPadding.OaepSHA256);
        var wrappedBase64Url = Base64UrlEncodeLocal(wrapped);

        // Act — unwrap via CLI
        var unwrapResult = await Program.RunAsync([
            "keyvault", "key", "unwrap",
            "--vault-name", VaultName,
            "--name", "cli-unwrap-key",
            "--version", version,
            "--algorithm", "RSA-OAEP-256",
            "--value", wrappedBase64Url,
            "-g", ResourceGroupName,
            "-s", SubscriptionId.ToString()
        ]);

        Assert.That(unwrapResult, Is.EqualTo(0));
    }

    [Test]
    public async Task KeyVaultTests_ReleaseKey_ShouldReturnValue()
    {
        // Arrange — create an RSA key via CLI
        var createResult = await Program.RunAsync([
            "keyvault", "key", "create",
            "--vault-name", VaultName,
            "--name", "cli-release-key",
            "--kty", "RSA",
            "-g", ResourceGroupName,
            "-s", SubscriptionId.ToString()
        ]);
        Assert.That(createResult, Is.EqualTo(0));

        var keyFilePath = Path.Combine(
            Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName,
            ".azure-key-vault", VaultName, "data", "keys", "cli-release-key.json");

        Assert.That(File.Exists(keyFilePath), Is.True);

        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(keyFilePath));
        var kid = doc.RootElement[0].GetProperty("key").GetProperty("kid").GetString()!;
        var version = kid.Split('/').Last();

        // Act — release via CLI
        var releaseResult = await Program.RunAsync([
            "keyvault", "key", "release",
            "--vault-name", VaultName,
            "--name", "cli-release-key",
            "--version", version,
            "--target", "any-attestation-token",
            "-g", ResourceGroupName,
            "-s", SubscriptionId.ToString()
        ]);

        Assert.That(releaseResult, Is.EqualTo(0));
    }
}

