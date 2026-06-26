using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Core;
using Topaz.Chaos;
using Topaz.Identity;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

public class ChaosInjectionTests
{
    private static readonly HttpClient HttpClient = new();
    private static readonly string ChaosBaseUrl =
        $"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/topaz/chaos";
    private static readonly string RulesUrl = $"{ChaosBaseUrl}/rules";

    // Non-Topaz ARM endpoint used as injection target — chaos fires here.
    private static readonly string TargetUrl =
        $"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/subscriptions";

    // Topaz-namespaced endpoint — always exempt from chaos injection.
    private static readonly string TopazTargetUrl = RulesUrl;

    private static async Task<HttpResponseMessage> CallTargetAsync()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var token = await credential.GetTokenAsync(
            new TokenRequestContext(["https://management.azure.com/.default"]),
            CancellationToken.None);
        var request = new HttpRequestMessage(HttpMethod.Get, TargetUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return await HttpClient.SendAsync(request);
    }

    private string _ruleId = string.Empty;

    [SetUp]
    public async Task SetUp()
    {
        _ruleId = $"inj-rule-{Guid.NewGuid():N}";
        await HttpClient.PostAsync($"{ChaosBaseUrl}/enable", null);
    }

    [TearDown]
    public async Task TearDown()
    {
        // Bypass the chaos pipeline entirely — set the flag directly so cleanup HTTP
        // calls are never faulted regardless of what rules are still active.
        ChaosStateProvider.IsEnabled = false;
        await HttpClient.DeleteAsync($"{RulesUrl}/{_ruleId}");
    }

    // -------------------------------------------------------------------------
    // Fault type tests — chaos globally enabled, rule with faultRate≈1.0
    // -------------------------------------------------------------------------

    [Test]
    public async Task ChaosInjection_TransientError_Returns500WithAzureErrorBody()
    {
        await HttpClient.PutAsJsonAsync($"{RulesUrl}/{_ruleId}",
            new { serviceNamespace = "*", faultType = "TransientError", faultRate = 0.9999 },
            GlobalSettings.JsonOptions);

        var response = await CallTargetAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Multiple(() =>
        {
            Assert.That(doc.RootElement.TryGetProperty("error", out var error), Is.True);
            Assert.That(error.GetProperty("code").GetString(), Is.EqualTo("InternalServerError"));
            Assert.That(error.GetProperty("message").GetString(), Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task ChaosInjection_Throttle_Returns429WithRetryAfterHeader()
    {
        await HttpClient.PutAsJsonAsync($"{RulesUrl}/{_ruleId}",
            new { serviceNamespace = "*", faultType = "Throttle", faultRate = 0.9999 },
            GlobalSettings.JsonOptions);

        var response = await CallTargetAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        Assert.That(response.Headers.RetryAfter, Is.Not.Null);
    }

    // -------------------------------------------------------------------------
    // Chaos globally disabled — rule exists but engine is off
    // -------------------------------------------------------------------------

    [Test]
    public async Task ChaosInjection_ChaosDisabled_RequestSucceeds()
    {
        await HttpClient.PutAsJsonAsync($"{RulesUrl}/{_ruleId}",
            new { serviceNamespace = "*", faultType = "TransientError", faultRate = 0.9999 },
            GlobalSettings.JsonOptions);

        // Disable chaos globally BEFORE making the target call.
        ChaosStateProvider.IsEnabled = false;

        var response = await CallTargetAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    // -------------------------------------------------------------------------
    // Rule-level enabled flag — individual rule disabled while chaos is on
    // -------------------------------------------------------------------------

    [Test]
    public async Task ChaosInjection_RuleDisabled_RequestSucceeds()
    {
        await HttpClient.PutAsJsonAsync($"{RulesUrl}/{_ruleId}",
            new { serviceNamespace = "*", faultType = "TransientError", faultRate = 0.9999 },
            GlobalSettings.JsonOptions);

        // Disable the individual rule while chaos remains globally enabled.
        // This call goes through the chaos pipeline but with faultRate=0.9999 it will
        // almost certainly be faulted — so we disable chaos first, disable the rule,
        // then re-enable chaos to prove the rule-level flag is respected.
        ChaosStateProvider.IsEnabled = false;
        await HttpClient.PostAsync($"{RulesUrl}/{_ruleId}/disable", null);
        ChaosStateProvider.IsEnabled = true;

        var response = await CallTargetAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    // -------------------------------------------------------------------------
    // faultRate = 0.0 — rule can never fire
    // -------------------------------------------------------------------------

    [Test]
    public async Task ChaosInjection_ZeroFaultRate_NeverFires()
    {
        await HttpClient.PutAsJsonAsync($"{RulesUrl}/{_ruleId}",
            new { serviceNamespace = "*", faultType = "TransientError", faultRate = 0.0 },
            GlobalSettings.JsonOptions);

        for (var i = 0; i < 5; i++)
        {
            var response = await CallTargetAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"Unexpected fault on attempt {i + 1}");
        }
    }

    // -------------------------------------------------------------------------
    // serviceNamespace filtering — non-matching namespace must not inject a fault
    // -------------------------------------------------------------------------

    [Test]
    public async Task ChaosInjection_ServiceNamespace_NonMatchingNamespace_DoesNotFire()
    {
        // Rule scoped to Microsoft.Compute — the target URL (/subscriptions) has no Compute path segment.
        await HttpClient.PutAsJsonAsync($"{RulesUrl}/{_ruleId}",
            new { serviceNamespace = "Microsoft.Compute", faultType = "TransientError", faultRate = 0.9999 },
            GlobalSettings.JsonOptions);

        var response = await CallTargetAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ChaosInjection_ServiceNamespaceWildcard_FiresForAllEndpoints()
    {
        await HttpClient.PutAsJsonAsync($"{RulesUrl}/{_ruleId}",
            new { serviceNamespace = "*", faultType = "Throttle", faultRate = 0.9999 },
            GlobalSettings.JsonOptions);

        var response = await CallTargetAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
    }

    // -------------------------------------------------------------------------
    // Topaz endpoints are exempt — chaos never fires on /topaz/* paths
    // -------------------------------------------------------------------------

    [Test]
    public async Task ChaosInjection_TopazEndpointsAreExempt_FromChaosInjection()
    {
        // Wildcard rule with rate ≈ 1 — would fire on any non-Topaz endpoint.
        await HttpClient.PutAsJsonAsync($"{RulesUrl}/{_ruleId}",
            new { serviceNamespace = "*", faultType = "TransientError", faultRate = 0.9999 },
            GlobalSettings.JsonOptions);

        // Call a Topaz-namespaced endpoint directly — must return 200 despite the active rule.
        var response = await HttpClient.GetAsync(TopazTargetUrl);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
