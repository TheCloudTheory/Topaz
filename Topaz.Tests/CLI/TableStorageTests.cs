namespace Topaz.Tests.CLI;

public class TableStorageTests
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

        await Program.Main(
        [
            "subscription",
            "create",
            "--id",
            Guid.Empty.ToString(),
            "--name",
            "sub-test"
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
            "storage",
            "account",
            "delete",
            "--name",
            "test"
        ]);

        await Program.Main([
            "storage",
            "account",
            "create",
            "--name",
            "test",
            "-g",
            "test",
            "--location",
            "westeurope",
            "--subscriptionId",
            Guid.Empty.ToString()
        ]);
    }
    
    [Test]
    public async Task TableStorageTests_WhenNewTableIsRequested_ItShouldBeCreated()
    {
        var tableDirectoryPath = Path.Combine(".topaz", ".azure-storage", "test", ".table", "test", "metadata.json");
        
        await Program.Main([
            "storage",
            "table",
            "create",
            "--name",
            "test",
            "--account-name",
            "test"
        ]);

        Assert.That(File.Exists(tableDirectoryPath), Is.True);
    }
}