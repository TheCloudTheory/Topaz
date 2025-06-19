using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class ResourceGroupTests
{
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    private static readonly string ResourceGroupName = "test";
    
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
            "sub-test"
        ]);

        await Program.Main([
            "group",
            "delete",
            "--name",
            ResourceGroupName
        ]);

        await Program.Main([
            "group",
            "create",
            "--name",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString(),
        ]);
    }

    [Test]
    public void ResourceGroupTests_WhenNewResourceGroupIsRequested_ItShouldBeCreated()
    {
        var resourceGroupPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".resource-groups", ResourceGroupName, "metadata.json");

        Assert.That(File.Exists(resourceGroupPath), Is.True);
    }

    [Test]
    public async Task ResourceGroupTests_WhenNewResourceGroupIsDeleted_ItShouldBeDeleted()
    {
        var resourceGroupPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".resource-groups", ResourceGroupName, "metadata.json");

        var code = await Program.Main([
            "group",
            "delete",
            "--name",
            ResourceGroupName
        ]);
        
        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(resourceGroupPath), Is.False);
            Assert.That(code, Is.EqualTo(0));
        });
    }
    
    [Test]
    public async Task ResourceGroupTests_WhenDeletedResourceGroupDoesNotExists_ItShouldReportError()
    {
        var code = await Program.Main([
            "group",
            "delete",
            "--name",
            "invalid-resource-group"
        ]);
        
        Assert.That(code, Is.EqualTo(1));
    }

    [Test]
    public async Task ResourceGroupTests_WhenResourceGroupsAreListed_CommandShouldExecuteSuccessfully()
    {
        var code = await Program.Main([
            "group",
            "list",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        Assert.That(code, Is.EqualTo(0));
    }
}
