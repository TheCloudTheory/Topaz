using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
    private static readonly HttpClient Http = new();

    private const string SubscriptionName = "sub-test-appservice-kudu";
    private const string ResourceGroupName = "rg-test-appservice-kudu";
    private const string PlanName = "plan-test-kudu";

    private static HttpClient CreateKuduHttpClient() => new();

    private static async Task<(string userName, string password)> GetPublishingCredentials(string siteName)
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var token = (await credential.GetTokenAsync(new TokenRequestContext([]), CancellationToken.None)).Token;

        var url = $"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.Web/sites/{siteName}/config/publishingcredentials/list?api-version=2022-03-01";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var first = doc.RootElement;
        var props = first.GetProperty("properties");
        return (props.GetProperty("publishingUserName").GetString()!, props.GetProperty("publishingPassword").GetString()!);
    }

    private static void SetBasicAuth(HttpRequestMessage request, string userName, string password)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);
    }

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
        var resourceGroup = (await (await armClient.GetDefaultSubscriptionAsync()).GetResourceGroupAsync(ResourceGroupName)).Value;

        var planData = new AppServicePlanData(AzureLocation.WestEurope)
        {
            Sku = new AppServiceSkuDescription { Name = "B1", Tier = "Basic", Capacity = 1 }
        };
        await resourceGroup.GetAppServicePlans().CreateOrUpdateAsync(WaitUntil.Completed, PlanName, planData);
    }

    [Test]
    public async Task ZipDeploy_ReturnsAcceptedWithLocationHeader()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = (await (await armClient.GetDefaultSubscriptionAsync()).GetResourceGroupAsync(ResourceGroupName)).Value;

        var siteData = new WebSiteData(AzureLocation.WestEurope) { Kind = "app" };
        await resourceGroup.GetWebSites().CreateOrUpdateAsync(WaitUntil.Completed, "kudu-test-deploy", siteData);

        var (userName, password) = await GetPublishingCredentials("kudu-test-deploy");

        using var http = CreateKuduHttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{KuduBaseUrl("kudu-test-deploy")}/api/zipdeploy")
        {
            Content = new ByteArrayContent([])
        };
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        SetBasicAuth(request, userName, password);

        var response = await http.SendAsync(request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(response.Headers.Location, Is.Not.Null);
        }
        
        Assert.That(response.Headers.Location!.ToString(), Does.StartWith("/api/deployments/"));
    }

    [Test]
    public async Task GetDeployments_AfterZipDeploy_ReturnsList()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = (await (await armClient.GetDefaultSubscriptionAsync()).GetResourceGroupAsync(ResourceGroupName)).Value;

        var siteData = new WebSiteData(AzureLocation.WestEurope) { Kind = "app" };
        await resourceGroup.GetWebSites().CreateOrUpdateAsync(WaitUntil.Completed, "kudu-test-list", siteData);

        var (userName, password) = await GetPublishingCredentials("kudu-test-list");

        using var http = CreateKuduHttpClient();
        var deployRequest = new HttpRequestMessage(HttpMethod.Post, $"{KuduBaseUrl("kudu-test-list")}/api/zipdeploy")
        {
            Content = new ByteArrayContent([])
        };
        deployRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        SetBasicAuth(deployRequest, userName, password);
        var deployResponse = await http.SendAsync(deployRequest);
        Assert.That(deployResponse.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

        var listRequest = new HttpRequestMessage(HttpMethod.Get, $"{KuduBaseUrl("kudu-test-list")}/api/deployments");
        SetBasicAuth(listRequest, userName, password);
        var listResponse = await http.SendAsync(listRequest);
        Assert.That(listResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await listResponse.Content.ReadAsStringAsync();
        var records = JsonDocument.Parse(body).RootElement;
        Assert.That(records.GetArrayLength(), Is.GreaterThanOrEqualTo(1));

        var first = records[0];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.GetProperty("id").GetString(), Is.Not.Null.And.Not.Empty);
            Assert.That(first.GetProperty("status").GetString(), Is.EqualTo("succeeded"));
            Assert.That(first.GetProperty("deployer").GetString(), Is.EqualTo("Push Deployer"));
        }
    }

    [Test]
    public async Task GetDeploymentById_ReturnsRecord()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = (await (await armClient.GetDefaultSubscriptionAsync()).GetResourceGroupAsync(ResourceGroupName)).Value;

        var siteData = new WebSiteData(AzureLocation.WestEurope) { Kind = "app" };
        await resourceGroup.GetWebSites().CreateOrUpdateAsync(WaitUntil.Completed, "kudu-test-getbyid", siteData);

        var (userName, password) = await GetPublishingCredentials("kudu-test-getbyid");

        using var http = CreateKuduHttpClient();
        var deployRequest = new HttpRequestMessage(HttpMethod.Post, $"{KuduBaseUrl("kudu-test-getbyid")}/api/zipdeploy")
        {
            Content = new ByteArrayContent([])
        };
        deployRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        SetBasicAuth(deployRequest, userName, password);
        var deployResponse = await http.SendAsync(deployRequest);
        Assert.That(deployResponse.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

        var location = deployResponse.Headers.Location!.ToString(); // e.g. /api/deployments/{id}
        var getRequest = new HttpRequestMessage(HttpMethod.Get, $"{KuduBaseUrl("kudu-test-getbyid")}{location}");
        SetBasicAuth(getRequest, userName, password);
        var getResponse = await http.SendAsync(getRequest);
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await getResponse.Content.ReadAsStringAsync();
        var record = JsonDocument.Parse(body).RootElement;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(record.GetProperty("status").GetString(), Is.EqualTo("succeeded"));
            Assert.That(record.GetProperty("deployer").GetString(), Is.EqualTo("Push Deployer"));
        }
    }

    [Test]
    public async Task GetDeploymentById_UnknownId_Returns404()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = (await (await armClient.GetDefaultSubscriptionAsync()).GetResourceGroupAsync(ResourceGroupName)).Value;

        var siteData = new WebSiteData(AzureLocation.WestEurope) { Kind = "app" };
        await resourceGroup.GetWebSites().CreateOrUpdateAsync(WaitUntil.Completed, "kudu-test-getbyid-404", siteData);

        var (userName, password) = await GetPublishingCredentials("kudu-test-getbyid-404");

        using var http = CreateKuduHttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{KuduBaseUrl("kudu-test-getbyid-404")}/api/deployments/{Guid.NewGuid()}");
        SetBasicAuth(request, userName, password);
        var response = await http.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetDeployments_WhenNoDeployments_ReturnsEmptyArray()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = (await (await armClient.GetDefaultSubscriptionAsync()).GetResourceGroupAsync(ResourceGroupName)).Value;

        var siteData = new WebSiteData(AzureLocation.WestEurope) { Kind = "app" };
        await resourceGroup.GetWebSites().CreateOrUpdateAsync(WaitUntil.Completed, "kudu-test-empty", siteData);

        var (userName, password) = await GetPublishingCredentials("kudu-test-empty");

        using var http = CreateKuduHttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{KuduBaseUrl("kudu-test-empty")}/api/deployments");
        SetBasicAuth(request, userName, password);
        var response = await http.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadAsStringAsync();
        var records = JsonDocument.Parse(body).RootElement;
        Assert.That(records.GetArrayLength(), Is.EqualTo(0));
    }

    [Test]
    public async Task ZipDeploy_WithoutCredentials_Returns401WithWwwAuthenticateHeader()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = (await (await armClient.GetDefaultSubscriptionAsync()).GetResourceGroupAsync(ResourceGroupName)).Value;

        var siteData = new WebSiteData(AzureLocation.WestEurope) { Kind = "app" };
        await resourceGroup.GetWebSites().CreateOrUpdateAsync(WaitUntil.Completed, "kudu-test-unauth", siteData);

        using var http = CreateKuduHttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{KuduBaseUrl("kudu-test-unauth")}/api/zipdeploy")
        {
            Content = new ByteArrayContent([])
        };
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

        var response = await http.SendAsync(request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(response.Headers.WwwAuthenticate.ToString(), Does.Contain("Basic realm=\"Kudu\""));
        }
    }

    [Test]
    public async Task ZipDeploy_WithWrongCredentials_Returns401()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var resourceGroup = (await (await armClient.GetDefaultSubscriptionAsync()).GetResourceGroupAsync(ResourceGroupName)).Value;

        var siteData = new WebSiteData(AzureLocation.WestEurope) { Kind = "app" };
        await resourceGroup.GetWebSites().CreateOrUpdateAsync(WaitUntil.Completed, "kudu-test-wrong-creds", siteData);

        using var http = CreateKuduHttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{KuduBaseUrl("kudu-test-wrong-creds")}/api/zipdeploy")
        {
            Content = new ByteArrayContent([])
        };
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        SetBasicAuth(request, "wrong-user", "wrong-password");

        var response = await http.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
