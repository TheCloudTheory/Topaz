using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class AppServicePlanTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("D4E5F600-0000-0000-0000-AB0100000001");

    private const string SubscriptionName = "sub-test-appservice";
    private const string ResourceGroupName = "rg-test-appservice";

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
    }

    [Test]
    public void AppServicePlan_CreateOrUpdate_ReturnsCreatedResource()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

        var planData = new AppServicePlanData(AzureLocation.WestEurope)
        {
            Sku = new AppServiceSkuDescription { Name = "B1", Tier = "Basic", Capacity = 1 }
        };

        resourceGroup.GetAppServicePlans()
            .CreateOrUpdate(WaitUntil.Completed, "test-plan-sdk", planData);

        var plan = resourceGroup.GetAppServicePlan("test-plan-sdk").Value;
        Assert.That(plan.Data.Name, Is.EqualTo("test-plan-sdk"));
        Assert.That(plan.Data.Sku!.Name, Is.EqualTo("B1"));
    }

    [Test]
    public void AppServicePlan_Get_ReturnsResource()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

        var planData = new AppServicePlanData(AzureLocation.WestEurope)
        {
            Sku = new AppServiceSkuDescription { Name = "S1", Tier = "Standard", Capacity = 1 }
        };
        resourceGroup.GetAppServicePlans().CreateOrUpdate(WaitUntil.Completed, "test-plan-get", planData);

        var plan = resourceGroup.GetAppServicePlan("test-plan-get").Value;

        Assert.That(plan.Data.Name, Is.EqualTo("test-plan-get"));
        Assert.That(plan.Data.Sku!.Name, Is.EqualTo("S1"));
    }

    [Test]
    public void AppServicePlan_Delete_RemovesResource()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

        var planData = new AppServicePlanData(AzureLocation.WestEurope)
        {
            Sku = new AppServiceSkuDescription { Name = "B1", Tier = "Basic", Capacity = 1 }
        };
        resourceGroup.GetAppServicePlans()
            .CreateOrUpdate(WaitUntil.Completed, "test-plan-delete", planData);
        var plan = resourceGroup.GetAppServicePlan("test-plan-delete").Value;

        plan.Delete(WaitUntil.Completed);

        Assert.Throws<RequestFailedException>(() => resourceGroup.GetAppServicePlan("test-plan-delete").Value.ToString());
    }

    [Test]
    public void AppServicePlan_ListByResourceGroup_ReturnsPlans()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

        var planData = new AppServicePlanData(AzureLocation.WestEurope)
        {
            Sku = new AppServiceSkuDescription { Name = "B1", Tier = "Basic", Capacity = 1 }
        };
        resourceGroup.GetAppServicePlans().CreateOrUpdate(WaitUntil.Completed, "test-plan-list1", planData);
        resourceGroup.GetAppServicePlans().CreateOrUpdate(WaitUntil.Completed, "test-plan-list2", planData);

        var plans = resourceGroup.GetAppServicePlans().GetAll().ToList();

        Assert.That(plans.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(plans.Any(p => p.Data.Name == "test-plan-list1"), Is.True);
        Assert.That(plans.Any(p => p.Data.Name == "test-plan-list2"), Is.True);
    }

    [Test]
    public void AppServicePlan_ListBySubscription_ReturnsPlans()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

        var planData = new AppServicePlanData(AzureLocation.WestEurope)
        {
            Sku = new AppServiceSkuDescription { Name = "B1", Tier = "Basic", Capacity = 1 }
        };
        resourceGroup.GetAppServicePlans().CreateOrUpdate(WaitUntil.Completed, "test-plan-sub-list", planData);

        var plans = armClient.GetDefaultSubscription().GetAppServicePlans().ToList();

        Assert.That(plans.Any(p => p.Data.Name == "test-plan-sub-list"), Is.True);
    }
}
