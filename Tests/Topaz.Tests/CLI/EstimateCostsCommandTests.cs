using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class EstimateCostsCommandTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("F1A2B3C4-D5E6-7890-ABCD-EF0011223355");
    private const string SubscriptionName = "sub-test-finops-cli";

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
    public async Task EstimateCosts_WhenSubscriptionExists_CommandShouldSucceed()
    {
        var exitCode = await Program.RunAsync([
            "finops", "estimate",
            "--subscription", SubscriptionId.ToString()
        ]);

        Assert.That(exitCode, Is.EqualTo(0));
    }

    [Test]
    public async Task EstimateCosts_WithCurrencyOption_CommandShouldSucceed()
    {
        var exitCode = await Program.RunAsync([
            "finops", "estimate",
            "--subscription", SubscriptionId.ToString(),
            "--currency", "EUR"
        ]);

        Assert.That(exitCode, Is.EqualTo(0));
    }

    [Test]
    public async Task EstimateCosts_WithJsonOutput_CommandShouldSucceed()
    {
        var exitCode = await Program.RunAsync([
            "finops", "estimate",
            "--subscription", SubscriptionId.ToString(),
            "--output", "json"
        ]);

        Assert.That(exitCode, Is.EqualTo(0));
    }
}
