using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

public class AppServiceSiteTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("D4E5F600-0000-0000-0000-AB0100000002");

    private const string SubscriptionName = "sub-test-appservice-site";
    private const string ResourceGroupName = "rg-test-appservice-site";
    private const string PlanName = "plan-test-site";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(
        [
            "subscription",
            "delete",
            "--id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "subscription",
            "create",
            "--id",
            SubscriptionId.ToString(),
            "--name",
            SubscriptionName
        ]);

        await Program.RunAsync([
            "group",
            "delete",
            "--name",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "group",
            "create",
            "--name",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

        var planData = new AppServicePlanData(AzureLocation.WestEurope)
        {
            Sku = new AppServiceSkuDescription { Name = "B1", Tier = "Basic", Capacity = 1 }
        };
        resourceGroup.GetAppServicePlans().CreateOrUpdate(WaitUntil.Completed, PlanName, planData);
    }

    [Test]
    public void WebSite_CreateOrUpdate_ReturnsCreatedResource()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

        var siteData = new WebSiteData(AzureLocation.WestEurope) { Kind = "app" };

        var operation = resourceGroup.GetWebSites().CreateOrUpdate(WaitUntil.Completed, "test-site-create", siteData);

        Assert.That(operation.Value.Data.Name, Is.EqualTo("test-site-create"));
        Assert.That(operation.Value.Data.DefaultHostName, Is.EqualTo($"test-site-create.{GlobalSettings.AzureWebsitesDnsSuffix}"));
        Assert.That(operation.Value.Data.State, Is.EqualTo("Running"));
        Assert.That(operation.Value.Data.AvailabilityState, Is.EqualTo(WebSiteAvailabilityState.Normal));
    }

    [Test]
    public void WebSite_Get_ReturnsResource()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

        var siteData = new WebSiteData(AzureLocation.WestEurope) { Kind = "functionapp" };
        resourceGroup.GetWebSites().CreateOrUpdate(WaitUntil.Completed, "test-site-get", siteData);

        var site = resourceGroup.GetWebSite("test-site-get").Value;

        Assert.That(site.Data.Name, Is.EqualTo("test-site-get"));
        Assert.That(site.Data.Kind, Is.EqualTo("functionapp"));
        Assert.That(site.Data.DefaultHostName, Is.EqualTo($"test-site-get.{GlobalSettings.AzureWebsitesDnsSuffix}"));
    }

    [Test]
    public void WebSite_Delete_RemovesResource()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

        var siteData = new WebSiteData(AzureLocation.WestEurope) { Kind = "app" };
        resourceGroup.GetWebSites().CreateOrUpdate(WaitUntil.Completed, "test-site-delete", siteData);
        var site = resourceGroup.GetWebSite("test-site-delete").Value;

        site.Delete(WaitUntil.Completed);

        Assert.Throws<RequestFailedException>(() => resourceGroup.GetWebSite("test-site-delete").Value.ToString());
    }

    [Test]
    public void WebSite_ListByResourceGroup_ReturnsSites()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

        var siteData = new WebSiteData(AzureLocation.WestEurope) { Kind = "app" };
        resourceGroup.GetWebSites().CreateOrUpdate(WaitUntil.Completed, "test-site-list1", siteData);
        resourceGroup.GetWebSites().CreateOrUpdate(WaitUntil.Completed, "test-site-list2", siteData);

        var sites = resourceGroup.GetWebSites().GetAll().ToList();

        Assert.That(sites.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(sites.Any(s => s.Data.Name == "test-site-list1"), Is.True);
        Assert.That(sites.Any(s => s.Data.Name == "test-site-list2"), Is.True);
    }

    [Test]
    public void WebSite_ListBySubscription_ReturnsSites()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

        var siteData = new WebSiteData(AzureLocation.WestEurope) { Kind = "app" };
        resourceGroup.GetWebSites().CreateOrUpdate(WaitUntil.Completed, "test-site-sub-list", siteData);

        var sites = armClient.GetDefaultSubscription().GetWebSites().ToList();

        Assert.That(sites.Any(s => s.Data.Name == "test-site-sub-list"), Is.True);
    }
}
