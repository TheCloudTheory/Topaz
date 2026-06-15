using System.Net;
using System.Net.Http.Headers;
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

public class AppServiceKuduTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("D4E5F600-0000-0000-0000-AB0100000099");

    private const string SubscriptionName = "sub-test-appservice-kudu";
    private const string ResourceGroupName = "rg-test-appservice-kudu";
    private const string PlanName = "plan-test-kudu";

    private static HttpClient CreateKuduHttpClient() => new();

    private static string KuduBaseUrl(string siteName) =>
        $"https://{siteName}.{GlobalSettings.AppServiceKuduDnsSuffix}:{GlobalSettings.DefaultAppServiceKuduPort}";

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
    public async Task ZipDeploy_ReturnsAcceptedWithLocationHeader()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

        var siteData = new WebSiteData(AzureLocation.WestEurope) { Kind = "app" };
        resourceGroup.GetWebSites().CreateOrUpdate(WaitUntil.Completed, "kudu-test-deploy", siteData);

        using var http = CreateKuduHttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{KuduBaseUrl("kudu-test-deploy")}/api/zipdeploy")
        {
            Content = new ByteArrayContent([])
        };
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

        var response = await http.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        Assert.That(response.Headers.Location, Is.Not.Null);
        Assert.That(response.Headers.Location!.ToString(), Does.StartWith("/api/deployments/"));
    }

    [Test]
    public async Task GetDeployments_AfterZipDeploy_ReturnsList()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

        var siteData = new WebSiteData(AzureLocation.WestEurope) { Kind = "app" };
        resourceGroup.GetWebSites().CreateOrUpdate(WaitUntil.Completed, "kudu-test-list", siteData);

        using var http = CreateKuduHttpClient();
        var deployRequest = new HttpRequestMessage(HttpMethod.Post, $"{KuduBaseUrl("kudu-test-list")}/api/zipdeploy")
        {
            Content = new ByteArrayContent([])
        };
        deployRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        var deployResponse = await http.SendAsync(deployRequest);
        Assert.That(deployResponse.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

        var listResponse = await http.GetAsync($"{KuduBaseUrl("kudu-test-list")}/api/deployments");
        Assert.That(listResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await listResponse.Content.ReadAsStringAsync();
        var records = System.Text.Json.JsonDocument.Parse(body).RootElement;
        Assert.That(records.GetArrayLength(), Is.GreaterThanOrEqualTo(1));

        var first = records[0];
        Assert.That(first.GetProperty("id").GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(first.GetProperty("status").GetString(), Is.EqualTo("succeeded"));
        Assert.That(first.GetProperty("deployer").GetString(), Is.EqualTo("Push Deployer"));
    }

    [Test]
    public async Task GetDeployments_WhenNoDeployments_ReturnsEmptyArray()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(ResourceGroupName).Value;

        var siteData = new WebSiteData(AzureLocation.WestEurope) { Kind = "app" };
        resourceGroup.GetWebSites().CreateOrUpdate(WaitUntil.Completed, "kudu-test-empty", siteData);

        using var http = CreateKuduHttpClient();
        var response = await http.GetAsync($"{KuduBaseUrl("kudu-test-empty")}/api/deployments");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadAsStringAsync();
        var records = System.Text.Json.JsonDocument.Parse(body).RootElement;
        Assert.That(records.GetArrayLength(), Is.EqualTo(0));
    }
}
