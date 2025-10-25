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
            VaultName,
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString(),
        ]);
        
        await Program.Main([
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

        var result = await Program.Main([
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
        var result = await Program.Main([
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
        var result = await Program.Main([
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
}
