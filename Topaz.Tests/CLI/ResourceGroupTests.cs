using System.Text.Json;
using Topaz.CLI;
using Topaz.Service.ResourceGroup.Models;
using Topaz.Shared;

namespace Topaz.Tests.CLI;

public class ResourceGroupTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("831DA9D1-54A1-45B3-90D7-EE3BD2801362");
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string ResourceGroupName2 = "test";

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
            SubscriptionId.ToString(),
        ]);
    }

    [Test]
    public void ResourceGroupTests_WhenNewResourceGroupIsRequested_ItShouldBeCreated()
    {
        var resourceGroupPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, "metadata.json");

        Assert.That(File.Exists(resourceGroupPath), Is.True);
    }

    [Test]
    public async Task ResourceGroupTests_WhenNewResourceGroupIsDeleted_ItShouldBeDeleted()
    {
        var resourceGroupPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, "metadata.json");

        var code = await Program.Main([
            "group",
            "delete",
            "--name",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
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
            "invalid-resource-group",
            "--subscription-id",
            SubscriptionId.ToString()
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
    
    [Test]
    public async Task ResourceGroupTests_WhenResourceGroupsIsCreatedWithSpecificLocation_TheLocationShouldBeReturned()
    {
        await Program.Main([
            "group",
            "delete",
            "--name",
            ResourceGroupName2,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        var code = await Program.Main([
            "group",
            "create",
            "--name",
            ResourceGroupName2,
            "--location",
            "northeurope",
            "--subscription-id",
            SubscriptionId.ToString(),
        ]);
        
        Assert.That(code, Is.EqualTo(0));
        
        var code2 = await Program.Main([
            "group",
            "show",
            "--name",
            ResourceGroupName2,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        Assert.That(code2, Is.EqualTo(0));
        
        var resourceGroupPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription", SubscriptionId.ToString(), ".resource-group", ResourceGroupName2, "metadata.json");
        var metadata = await File.ReadAllTextAsync(resourceGroupPath);
        var rg = JsonSerializer.Deserialize<ResourceGroupResource>(metadata, GlobalSettings.JsonOptions);
        
        Assert.That(rg, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(rg.Name, Is.EqualTo(ResourceGroupName));
            Assert.That(rg.Location, Is.EqualTo("northeurope"));
        });
    }
    
    [Test]
    public async Task ResourceGroupTests_WhenResourceGroupIsGoingToBeCreatedInNonExistentSubscription_TheCommandMustFail()
    {
        var result = await Program.Main([
            "group",
            "create",
            "--name",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            Guid.NewGuid().ToString(),
        ]);

        Assert.That(result, Is.EqualTo(1));
    }
}
