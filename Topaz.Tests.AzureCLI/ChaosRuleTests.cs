namespace Topaz.Tests.AzureCLI;

public class ChaosRuleTests : TopazFixture
{
    private string _ruleId = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _ruleId = $"cli-rule-{Guid.NewGuid():N}";
    }

    [TearDown]
    public async Task TearDown()
    {
        try { await RunAzureCliCommand($"az rest --method delete --uri \"https://topaz.local.dev:8899/topaz/chaos/rules/{_ruleId}\""); }
        catch { /* ignored — rule may have been deleted by the test */ }
    }

    [Test]
    public async Task ChaosRule_Create_Returns201()
    {
        await RunAzureCliCommand(
            $"az rest --method put " +
            $"--uri \"https://topaz.local.dev:8899/topaz/chaos/rules/{_ruleId}\" " +
            $"--body '{{\"serviceNamespace\":\"Microsoft.Storage\",\"faultType\":\"Throttle\",\"faultRate\":0.5}}' " +
            $"--headers Content-Type=application/json",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["id"]!.GetValue<string>(), Is.EqualTo(_ruleId));
                    Assert.That(response["serviceNamespace"]!.GetValue<string>(), Is.EqualTo("Microsoft.Storage"));
                    Assert.That(response["enabled"]!.GetValue<bool>(), Is.True);
                });
            },
            0);
    }

    [Test]
    public async Task ChaosRule_CreateDuplicate_ReturnsBadRequest()
    {
        var body = $"'{{\"serviceNamespace\":\"*\",\"faultType\":\"Timeout\",\"faultRate\":0.1}}'";
        await RunAzureCliCommand(
            $"az rest --method put --uri \"https://topaz.local.dev:8899/topaz/chaos/rules/{_ruleId}\" --body {body} --headers Content-Type=application/json",
            null, 0);

        await RunAzureCliCommand(
            $"az rest --method put --uri \"https://topaz.local.dev:8899/topaz/chaos/rules/{_ruleId}\" --body {body} --headers Content-Type=application/json",
            null, 1);
    }

    [Test]
    public async Task ChaosRule_Get_ReturnsRule()
    {
        await RunAzureCliCommand(
            $"az rest --method put " +
            $"--uri \"https://topaz.local.dev:8899/topaz/chaos/rules/{_ruleId}\" " +
            $"--body '{{\"serviceNamespace\":\"Microsoft.KeyVault\",\"faultType\":\"TransientError\",\"faultRate\":0.2}}' " +
            $"--headers Content-Type=application/json",
            null, 0);

        await RunAzureCliCommand(
            $"az rest --method get --uri \"https://topaz.local.dev:8899/topaz/chaos/rules/{_ruleId}\"",
            response => Assert.That(response["id"]!.GetValue<string>(), Is.EqualTo(_ruleId)),
            0);
    }

    [Test]
    public async Task ChaosRule_List_ReturnsValueArray()
    {
        await RunAzureCliCommand(
            $"az rest --method put " +
            $"--uri \"https://topaz.local.dev:8899/topaz/chaos/rules/{_ruleId}\" " +
            $"--body '{{\"serviceNamespace\":\"*\",\"faultType\":\"ServiceUnavailable\",\"faultRate\":1.0}}' " +
            $"--headers Content-Type=application/json",
            null, 0);

        await RunAzureCliCommand(
            "az rest --method get --uri \"https://topaz.local.dev:8899/topaz/chaos/rules\"",
            response => Assert.That(response["value"], Is.Not.Null),
            0);
    }

    [Test]
    public async Task ChaosRule_Disable_SetsEnabledFalse()
    {
        await RunAzureCliCommand(
            $"az rest --method put " +
            $"--uri \"https://topaz.local.dev:8899/topaz/chaos/rules/{_ruleId}\" " +
            $"--body '{{\"serviceNamespace\":\"*\",\"faultType\":\"Throttle\",\"faultRate\":0.5}}' " +
            $"--headers Content-Type=application/json",
            null, 0);

        await RunAzureCliCommand(
            $"az rest --method post --uri \"https://topaz.local.dev:8899/topaz/chaos/rules/{_ruleId}/disable\"",
            response => Assert.That(response["enabled"]!.GetValue<bool>(), Is.False),
            0);
    }

    [Test]
    public async Task ChaosRule_Enable_SetsEnabledTrue()
    {
        await RunAzureCliCommand(
            $"az rest --method put " +
            $"--uri \"https://topaz.local.dev:8899/topaz/chaos/rules/{_ruleId}\" " +
            $"--body '{{\"serviceNamespace\":\"*\",\"faultType\":\"Throttle\",\"faultRate\":0.5}}' " +
            $"--headers Content-Type=application/json",
            null, 0);
        await RunAzureCliCommand(
            $"az rest --method post --uri \"https://topaz.local.dev:8899/topaz/chaos/rules/{_ruleId}/disable\"",
            null, 0);

        await RunAzureCliCommand(
            $"az rest --method post --uri \"https://topaz.local.dev:8899/topaz/chaos/rules/{_ruleId}/enable\"",
            response => Assert.That(response["enabled"]!.GetValue<bool>(), Is.True),
            0);
    }

    [Test]
    public async Task ChaosRule_Delete_Succeeds()
    {
        await RunAzureCliCommand(
            $"az rest --method put " +
            $"--uri \"https://topaz.local.dev:8899/topaz/chaos/rules/{_ruleId}\" " +
            $"--body '{{\"serviceNamespace\":\"*\",\"faultType\":\"Timeout\",\"faultRate\":1.0}}' " +
            $"--headers Content-Type=application/json",
            null, 0);

        await RunAzureCliCommand(
            $"az rest --method delete --uri \"https://topaz.local.dev:8899/topaz/chaos/rules/{_ruleId}\"",
            null, 0);
    }
}
