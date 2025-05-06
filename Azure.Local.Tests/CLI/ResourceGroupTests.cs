namespace Azure.Local.Tests.CLI;

public class ResourceGroupTests
{
    [Test]
    public async Task ResourceGroupTests_WhenNewResourceGroupIsRequested_ItShouldBeCreated()
    {
        var resourceGroupPath = Path.Combine(".resource-group", "test.json");

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
            Assert.That(Directory.Exists(resourceGroupPath), Is.True);
        });
    }
}
