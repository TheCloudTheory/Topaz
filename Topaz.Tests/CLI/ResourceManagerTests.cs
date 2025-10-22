using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class ResourceManagerTests
{
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    private const string ResourceGroupName = "test";
    private const string DeploymentName = "TestDeployment";

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
            SubscriptionId.ToString()
        ]);
    }
    
    [Test]
    public async Task ResourceManagerTests_WhenNewDeploymentIsCreatedWithExplicitName_ItShouldBeCreated()
    {
        var result = await Program.Main([
            "deployment",
            "group",
            "create",
            "--resource-group",
            ResourceGroupName,
            "--name",
            DeploymentName
        ]);
        
        var deploymentPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".resource-manager", DeploymentName, "metadata.json");
        
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(File.Exists(deploymentPath), Is.True);
        });
    }

    [Test]
    public async Task ResourceManagerTests_WhenNewDeploymentIsCreatedWithNoExplicitNameAndNoTemplate_ItShouldBeCreatedAndNameShouldBeGeneratedAutomatically()
    {
        var result = await Program.Main([
            "deployment",
            "group",
            "create",
            "--resource-group",
            ResourceGroupName
        ]);
        
        var deploymentPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".resource-manager", "empty-template", "metadata.json");
        
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(File.Exists(deploymentPath), Is.True);
        });
    }
}