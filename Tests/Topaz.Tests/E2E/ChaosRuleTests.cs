using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

public class ChaosRuleTests
{
    private static readonly HttpClient HttpClient = new();
    private static readonly string BaseUrl =
        $"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/topaz/chaos/rules";

    private string _ruleId = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _ruleId = $"e2e-rule-{Guid.NewGuid():N}";
    }

    [TearDown]
    public async Task TearDown()
    {
        await HttpClient.DeleteAsync($"{BaseUrl}/{_ruleId}");
    }

    [Test]
    public async Task ChaosRule_Create_Returns201WithRuleBody()
    {
        var response = await HttpClient.PutAsJsonAsync($"{BaseUrl}/{_ruleId}", new
        {
            serviceNamespace = "Microsoft.Storage",
            faultType = "Throttle",
            faultRate = 0.5
        }, GlobalSettings.JsonOptions);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("id").GetString(), Is.EqualTo(_ruleId));
            Assert.That(root.GetProperty("serviceNamespace").GetString(), Is.EqualTo("Microsoft.Storage"));
            Assert.That(root.GetProperty("faultType").GetString(), Is.EqualTo("Throttle").IgnoreCase);
            Assert.That(root.GetProperty("faultRate").GetDouble(), Is.EqualTo(0.5));
            Assert.That(root.GetProperty("enabled").GetBoolean(), Is.True);
        });
    }

    [Test]
    public async Task ChaosRule_CreateDuplicate_Returns400WithDescriptiveError()
    {
        var body = new { serviceNamespace = "*", faultType = "Timeout", faultRate = 0.1 };
        await HttpClient.PutAsJsonAsync($"{BaseUrl}/{_ruleId}", body, GlobalSettings.JsonOptions);

        var response = await HttpClient.PutAsJsonAsync($"{BaseUrl}/{_ruleId}", body, GlobalSettings.JsonOptions);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        Assert.That(doc.RootElement.GetProperty("error").GetProperty("code").GetString(),
            Is.EqualTo("RuleAlreadyExists"));
    }

    [Test]
    public async Task ChaosRule_Get_Returns200WithRule()
    {
        await HttpClient.PutAsJsonAsync($"{BaseUrl}/{_ruleId}",
            new { serviceNamespace = "Microsoft.KeyVault", faultType = "TransientError", faultRate = 0.2 },
            GlobalSettings.JsonOptions);

        var response = await HttpClient.GetAsync($"{BaseUrl}/{_ruleId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.That(doc.RootElement.GetProperty("id").GetString(), Is.EqualTo(_ruleId));
    }

    [Test]
    public async Task ChaosRule_GetNonExistent_Returns404()
    {
        var response = await HttpClient.GetAsync($"{BaseUrl}/nonexistent-rule-{Guid.NewGuid():N}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ChaosRule_Delete_Returns204()
    {
        await HttpClient.PutAsJsonAsync($"{BaseUrl}/{_ruleId}",
            new { serviceNamespace = "*", faultType = "ServiceUnavailable", faultRate = 1.0 },
            GlobalSettings.JsonOptions);

        var response = await HttpClient.DeleteAsync($"{BaseUrl}/{_ruleId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task ChaosRule_DeleteNonExistent_Returns404()
    {
        var response = await HttpClient.DeleteAsync($"{BaseUrl}/nonexistent-rule-{Guid.NewGuid():N}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ChaosRule_List_Returns200WithValueArray()
    {
        await HttpClient.PutAsJsonAsync($"{BaseUrl}/{_ruleId}",
            new { serviceNamespace = "Microsoft.Storage", faultType = "Throttle", faultRate = 0.3 },
            GlobalSettings.JsonOptions);

        var response = await HttpClient.GetAsync(BaseUrl);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.That(doc.RootElement.TryGetProperty("value", out var value), Is.True);
        Assert.That(value.ValueKind, Is.EqualTo(JsonValueKind.Array));
    }

    [Test]
    public async Task ChaosRule_Disable_SetsEnabledFalse()
    {
        await HttpClient.PutAsJsonAsync($"{BaseUrl}/{_ruleId}",
            new { serviceNamespace = "*", faultType = "Timeout", faultRate = 0.5 },
            GlobalSettings.JsonOptions);

        var response = await HttpClient.PostAsync($"{BaseUrl}/{_ruleId}/disable", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.That(doc.RootElement.GetProperty("enabled").GetBoolean(), Is.False);
    }

    [Test]
    public async Task ChaosRule_Enable_SetsEnabledTrue()
    {
        await HttpClient.PutAsJsonAsync($"{BaseUrl}/{_ruleId}",
            new { serviceNamespace = "*", faultType = "Timeout", faultRate = 0.5 },
            GlobalSettings.JsonOptions);
        await HttpClient.PostAsync($"{BaseUrl}/{_ruleId}/disable", null);

        var response = await HttpClient.PostAsync($"{BaseUrl}/{_ruleId}/enable", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.That(doc.RootElement.GetProperty("enabled").GetBoolean(), Is.True);
    }

    [Test]
    public async Task ChaosRule_EnableNonExistent_Returns404()
    {
        var response = await HttpClient.PostAsync($"{BaseUrl}/nonexistent-rule-{Guid.NewGuid():N}/enable", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
