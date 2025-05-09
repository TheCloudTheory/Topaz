namespace Azure.Local.Tests.CLI;

public class ResourceGroupTests
{
    [Test]
    public async Task ResourceGroupTests_WhenNewResourceGroupIsRequested_ItShouldBeCreated()
    {
        var resourceGroupPath = Path.Combine(Directory.GetCurrentDirectory(), ".resource-groups", "test.json");

        await Program.Main([
            "group",
            "delete",
            "--name",
            "test"
        ]);

        var result = await Program.Main([
            "group",
            "create",
            "--name",
            "test",
            "--location",
            "westeurope"
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(File.Exists(resourceGroupPath), Is.True);
        });
    }

    [Test]
    public async Task ResourceGroupTests_WhenNewResourceGroupIsDeleted_ItShouldBeDeleted()
    {
        var resourceGroupPath = Path.Combine(Directory.GetCurrentDirectory(), ".resource-groups", "test.json");

        await Program.Main([
            "group",
            "delete",
            "--name",
            "test"
        ]);

        var result = await Program.Main([
            "group",
            "create",
            "--name",
            "test",
            "--location",
            "westeurope"
        ]);

        await Program.Main([
            "group",
            "delete",
            "--name",
            "test"
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(File.Exists(resourceGroupPath), Is.False);
        });
    }
}
