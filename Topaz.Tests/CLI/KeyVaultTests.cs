namespace Topaz.Tests.CLI;

public class KeyVaultTests
{
    [SetUp]
    public async Task SetUp()
    {
        await Program.Main(
        [
            "subscription",
            "delete",
            "--id",
            Guid.Empty.ToString()
        ]);

        await Program.Main([
            "group",
            "delete",
            "--name",
            "test"
        ]);

        await Program.Main([
            "group",
            "create",
            "--name",
            "test",
            "--location",
            "westeurope",
            "--subscriptionId",
            Guid.Empty.ToString()
        ]);

        await Program.Main([
            "keyvault",
            "delete",
            "--name",
            "test"
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
            "--subscriptionId",
            Guid.Empty.ToString(),
        ]);
    }

    [Test]
    public void KeyVaultTests_WhenNewKeyVaultIsRequested_ItShouldBeCreated()
    {
        var keyVaultPath = Path.Combine(Directory.GetCurrentDirectory(), ".abazure", ".azure-key-vault", "test", "metadata.json");

        Assert.That(File.Exists(keyVaultPath), Is.True);
    }

    [Test]
    public async Task KeyVaultTests_WhenNewKeyVaultIsDeleted_ItShouldBeDeleted()
    {
        var keyVaultPath = Path.Combine(Directory.GetCurrentDirectory(), ".abazure", ".azure-key-vault", "test");

        var result = await Program.Main([
            "keyvault",
            "delete",
            "--name",
            "test"
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(Directory.Exists(keyVaultPath), Is.False);
        });
    }
}
