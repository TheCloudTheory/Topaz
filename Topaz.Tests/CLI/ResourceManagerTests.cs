using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class ResourceManagerTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("DD4517F3-2D72-4EF6-A85A-1910C24F4566");
    private const string ResourceGroupName = "test";
    private const string MultipleDeploymentsResourceGroupName = "rg-test-multiple";
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
            DeploymentName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        var deploymentPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".resource-manager", DeploymentName, "metadata.json");
        
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
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        var deploymentPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".resource-manager", "empty-template", "metadata.json");
        
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(File.Exists(deploymentPath), Is.True);
        });
    }
    
    [Test]

    public async Task
        ResourceManagerTests_WhenNewDeploymentIsCreatedWithWithEmptyTemplateFileProvidedAndNoNameProvided_ItShouldBeCreatedAndNameShouldBeGeneratedAutomatically()
    {
        var result = await Program.Main([
            "deployment",
            "group",
            "create",
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString(),
            "--template-file",
            "templates/deployment1.json"
        ]);
        
        var deploymentPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".resource-manager", "deployment1", "metadata.json");
        
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(File.Exists(deploymentPath), Is.True);
        });
    }

    [Test]
    public async Task ResourceManagerTests_WhenDeploymentIsRequestedForNotExistingResourceGroup_ItMustFail()
    {
        var result = await Program.Main([
            "deployment",
            "group",
            "create",
            "--resource-group",
            "some-random-rg",
            "--subscription-id",
            SubscriptionId.ToString(),
        ]);
        
        Assert.That(result, Is.EqualTo(1));
    }

    [Test] public async Task ResourceManagerTests_WhenThereAreMultipleDeploymentsAvailable_TheyShouldBeReturned()
    {
        await Program.Main([
            "group",
            "delete",
            "--name",
            MultipleDeploymentsResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        await Program.Main([
            "group",
            "create",
            "--name",
            MultipleDeploymentsResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        await Program.Main([
            "deployment",
            "group",
            "create",
            "--resource-group",
            MultipleDeploymentsResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString(),
            "--template-file",
            "templates/deployment1.json",
            "--name",
            "deployment1"
        ]);
        
        await Program.Main([
            "deployment",
            "group",
            "create",
            "--resource-group",
            MultipleDeploymentsResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString(),
            "--template-file",
            "templates/deployment1.json",
            "--name",
            "deployment2"
        ]);
        
        await Program.Main([
            "deployment",
            "group",
            "create",
            "--resource-group",
            MultipleDeploymentsResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString(),
            "--template-file",
            "templates/deployment1.json",
            "--name",
            "deployment3"
        ]);

        var deploymentPaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
                SubscriptionId.ToString(), ".resource-group", MultipleDeploymentsResourceGroupName, ".resource-manager", "deployment1",
                "metadata.json"),
            Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
                SubscriptionId.ToString(), ".resource-group", MultipleDeploymentsResourceGroupName, ".resource-manager", "deployment2",
                "metadata.json"),
            Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
                SubscriptionId.ToString(), ".resource-group", MultipleDeploymentsResourceGroupName, ".resource-manager", "deployment3",
                "metadata.json")
        };
        
        Assert.That(deploymentPaths.Select(File.Exists).All(p => p), Is.True);
        
        var result = await Program.Main([
            "deployment",
            "group",
            "list",
            "--resource-group",
            MultipleDeploymentsResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        Assert.That(result, Is.EqualTo(0));
    }
}