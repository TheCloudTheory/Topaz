using System.Text.Json.Nodes;
using Topaz.Chaos;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

public class HealthEndpointTests
{
    private static readonly HttpClient HttpClient = new();
    private static readonly string HealthUrl =
        $"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/health";
    private static readonly string ChaosEnableUrl =
        $"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/topaz/chaos/enable";

    [TearDown]
    public void TearDown()
    {
        ChaosStateProvider.IsEnabled = false;
    }

    [Test]
    public async Task Health_WhenHostIsRunning_ReturnsAllExpectedFields()
    {
        var response = await HttpClient.GetAsync(HealthUrl);
        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That((int)response.StatusCode, Is.EqualTo(200));
            Assert.That(json["status"]!.GetValue<string>(), Is.EqualTo("Healthy"));
            Assert.That(json["workingDirectory"]!.GetValue<string>(), Is.Not.Null.And.Not.Empty);
            Assert.That(json["version"]!.GetValue<string>(), Is.Not.Null.And.Not.Empty);
            Assert.That(json["chaosEnabled"], Is.Not.Null);
            Assert.That(json["runningMode"]!.GetValue<string>(), Is.EqualTo("Standalone"));
            Assert.That(json["httpsConnectProxyAvailable"]!.GetValue<bool>(), Is.True);
            Assert.That(json["acrDockerExecutorAvailable"]!.GetValue<bool>(), Is.True);
            Assert.That(json["backgroundServices"]!.AsArray(), Has.Count.EqualTo(5));

            // Wait for background services to execute and fetch the response again
            await Task.Delay(TimeSpan.FromSeconds(60));
            
            response = await HttpClient.GetAsync(HealthUrl);
            json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
            
            var backgroundServices = json["backgroundServices"]!.AsArray();
            foreach (var service in backgroundServices)
            {
                // Key Vault jobs are executed once every hour, so they are deliberately skipped
                if(!service!["name"]!.GetValue<string>().Contains("Key Vault"))
                {
                    Assert.That(service!["executedAt"]!.GetValue<string>(), Is.Not.Null.And.Not.Empty);
                }
            }
        }
    }

    [Test]
    public async Task Health_WhenChaosIsEnabled_ResponseIncludesChaosEnabledTrue()
    {
        await HttpClient.PostAsync(ChaosEnableUrl, null);

        var response = await HttpClient.GetAsync(HealthUrl);
        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;

        Assert.That(json["chaosEnabled"]!.GetValue<bool>(), Is.True);
    }
}
