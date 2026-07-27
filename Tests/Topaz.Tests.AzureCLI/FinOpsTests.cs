namespace Topaz.Tests.AzureCLI;

public class FinOpsTests : TopazFixture
{
    [Test]
    public async Task FinOps_GetEstimatedCosts_ReturnsTwoHundredWithValidJsonShape()
    {
        await RunAzureCliCommand(
            "az rest --method get " +
            "--uri \"https://topaz.local.dev:8899/topaz/subscriptions/$(az account show --query id -o tsv)/estimatedCosts\"",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["subscriptionId"], Is.Not.Null,
                        "Response must contain 'subscriptionId'");
                    Assert.That(response["currency"], Is.Not.Null,
                        "Response must contain 'currency'");
                    Assert.That(response["totalMonthlyCost"], Is.Not.Null,
                        "Response must contain 'totalMonthlyCost'");
                    Assert.That(response["resources"], Is.Not.Null,
                        "Response must contain 'resources'");
                    Assert.That(response["totalMonthlyCost"]!.GetValue<double>(), Is.GreaterThanOrEqualTo(0),
                        "totalMonthlyCost must be >= 0");
                });
            },
            0);
    }

    [Test]
    public async Task FinOps_GetEstimatedCosts_WithCurrencyParameter_ReturnsCurrencyInResponse()
    {
        await RunAzureCliCommand(
            "az rest --method get " +
            "--uri \"https://topaz.local.dev:8899/topaz/subscriptions/$(az account show --query id -o tsv)/estimatedCosts?currency=EUR\"",
            response =>
            {
                Assert.That(response["currency"]!.GetValue<string>(),
                    Is.EqualTo("EUR").IgnoreCase);
            },
            0);
    }
}
