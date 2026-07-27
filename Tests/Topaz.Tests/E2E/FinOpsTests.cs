using System.Net;
using System.Text.Json;
using Topaz.CLI;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

public class FinOpsTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("F1A2B3C4-D5E6-7890-ABCD-EF0011223344");
    private const string SubscriptionName = "sub-test-finops";

    private static readonly HttpClient HttpClient = new();

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync([
            "subscription", "create",
            "--id", SubscriptionId.ToString(),
            "--name", SubscriptionName
        ]);
    }

    [Test]
    public async Task FinOps_GetEstimatedCosts_ReturnsTwoHundredWithValidJsonShape()
    {
        // Arrange
        var url = $"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}" +
                  $"/topaz/subscriptions/{SubscriptionId}/estimatedCosts";

        // Act
        var response = await HttpClient.GetAsync(url);

        // Assert — 200 OK
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.Multiple(() =>
        {
            // Required top-level fields present
            Assert.That(root.TryGetProperty("subscriptionId", out _), Is.True,
                "Response must contain 'subscriptionId'");
            Assert.That(root.TryGetProperty("currency", out _), Is.True,
                "Response must contain 'currency'");
            Assert.That(root.TryGetProperty("totalMonthlyCost", out var totalEl), Is.True,
                "Response must contain 'totalMonthlyCost'");
            Assert.That(root.TryGetProperty("resources", out _), Is.True,
                "Response must contain 'resources'");

            // totalMonthlyCost is a non-negative number
            Assert.That(totalEl.GetDouble(), Is.GreaterThanOrEqualTo(0),
                "totalMonthlyCost must be >= 0");
        });
    }

    [Test]
    public async Task FinOps_GetEstimatedCosts_WithCurrencyParameter_ReturnsCurrencyInResponse()
    {
        // Arrange
        var url = $"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}" +
                  $"/topaz/subscriptions/{SubscriptionId}/estimatedCosts?currency=EUR";

        // Act
        var response = await HttpClient.GetAsync(url);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.That(root.TryGetProperty("currency", out var currencyEl), Is.True);
        Assert.That(currencyEl.GetString(), Is.EqualTo("EUR").IgnoreCase);
    }
}
