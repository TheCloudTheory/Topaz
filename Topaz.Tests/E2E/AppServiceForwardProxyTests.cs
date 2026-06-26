using System.Net;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Topaz.CLI;
using Topaz.ForwardProxy;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

public class AppServiceForwardProxyTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("D4E5F600-0000-0000-0000-AB0100000103");

    private const string SubscriptionName = "sub-test-appservice-fwd";
    private const string ResourceGroupName = "rg-test-appservice-fwd";
    private const string PlanName = "plan-test-fwd";

    private static HttpClient CreateHttpClient() => new();

    private static string ForwardProxyUrl(string siteName) =>
        $"https://{siteName}.{GlobalSettings.AzureWebsitesDnsSuffix}:{ForwardProxySettings.DefaultPort}";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

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
    public async Task ForwardProxy_UnregisteredSite_ReturnsNotFound()
    {
        using var http = CreateHttpClient();
        var response = await http.GetAsync($"{ForwardProxyUrl("fwd-nonexistent-site")}/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ForwardProxy_RegisteredSite_NoWebsitesPort_AttemptsForward()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

        resourceGroup.GetWebSites().CreateOrUpdate(WaitUntil.Completed, "fwd-site-default-port",
            new WebSiteData(AzureLocation.WestEurope) { Kind = "app" });

        using var http = CreateHttpClient();
        var response = await http.GetAsync($"{ForwardProxyUrl("fwd-site-default-port")}/");

        // The site was found in the control plane; forwarding was attempted.
        // The target (http://fwd-site-default-port:80) is not running in the test
        // environment so the proxy returns an error — but not 404 (which would mean
        // the site was not registered).
        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ForwardProxy_RegisteredSite_WithWebsitesPort_AttemptsForward()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

        resourceGroup.GetWebSites().CreateOrUpdate(WaitUntil.Completed, "fwd-site-custom-port",
            new WebSiteData(AzureLocation.WestEurope) { Kind = "app" });

        var site = resourceGroup.GetWebSite("fwd-site-custom-port").Value;
        var appSettings = new AppServiceConfigurationDictionary();
        appSettings.Properties.Add("WEBSITES_PORT", "9999");
        site.UpdateApplicationSettings(appSettings);

        using var http = CreateHttpClient();
        var response = await http.GetAsync($"{ForwardProxyUrl("fwd-site-custom-port")}/health");

        // The site was found and WEBSITES_PORT was read — forwarding attempted to port 9999.
        // Target is not running so the proxy returns an error — but not 404.
        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.NotFound));
    }
}
