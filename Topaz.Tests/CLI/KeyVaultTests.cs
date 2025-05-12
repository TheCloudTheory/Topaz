namespace Topaz.Tests.CLI;

public class KeyVaultTests
{
    [Test]
    public async Task KeyVaultTests_WhenNewKeyVaultIsRequested_ItShouldBeCreated()
    {
        var keyVaultPath = Path.Combine(Directory.GetCurrentDirectory(), ".azure-key-vault", "test.json");

        await Program.Main([
            "keyvault",
            "delete",
            "--name",
            "test"
        ]);

        await Program.Main([
            "group",
            "create",
            "--name",
            "rg-test",
            "--location",
            "westeurope"
        ]);

        var result = await Program.Main([
            "keyvault",
            "create",
            "--name",
            "test",
            "-g",
            "rg-test",
            "--location",
            "westeurope"
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(File.Exists(keyVaultPath), Is.True);
        });
    }

    [Test]
    public async Task KeyVaultTests_WhenNewKeyVaultIsDeleted_ItShouldBeDeleted()
    {
        var keyVaultPath = Path.Combine(Directory.GetCurrentDirectory(), ".azure-key-vault", "test.json");

        await Program.Main([
            "keyvault",
            "delete",
            "--name",
            "test"
        ]);

        await Program.Main([
            "group",
            "create",
            "--name",
            "rg-test",
            "--location",
            "westeurope"
        ]);

        var result = await Program.Main([
            "keyvault",
            "create",
            "--name",
            "test",
            "-g",
            "rg-test",
            "--location",
            "westeurope"
        ]);

        await Program.Main([
            "keyvault",
            "delete",
            "--name",
            "test"
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(File.Exists(keyVaultPath), Is.False);
        });
    }
}
