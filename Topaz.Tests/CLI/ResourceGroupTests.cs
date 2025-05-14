namespace Topaz.Tests.CLI;

public class ResourceGroupTests
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
            Guid.Empty.ToString(),
        ]);
    }

    [Test]
    public void ResourceGroupTests_WhenNewResourceGroupIsRequested_ItShouldBeCreated()
    {
        var resourceGroupPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".resource-groups", "test", "metadata.json");

        Assert.That(File.Exists(resourceGroupPath), Is.True);
    }

    [Test]
    public async Task ResourceGroupTests_WhenNewResourceGroupIsDeleted_ItShouldBeDeleted()
    {
        var resourceGroupPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".resource-groups", "test", "metadata.json");

        await Program.Main([
            "group",
            "delete",
            "--name",
            "test"
        ]);

        Assert.That(File.Exists(resourceGroupPath), Is.False);
    }
}
