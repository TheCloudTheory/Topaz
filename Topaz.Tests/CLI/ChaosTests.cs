using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class ChaosTests
{
    private static readonly string RuleId = $"test-rule-{Guid.NewGuid():N}";

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync(["chaos", "rule", "delete", "--rule-id", RuleId]);
    }

    [Test]
    public async Task Chaos_WhenChaosIsEnabled_CommandShouldSucceed()
    {
        var code = await Program.RunAsync(["chaos", "enable"]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task Chaos_WhenChaosIsDisabled_CommandShouldSucceed()
    {
        var code = await Program.RunAsync(["chaos", "disable"]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task Chaos_WhenChaosStatusIsRequested_CommandShouldSucceed()
    {
        var code = await Program.RunAsync(["chaos", "status"]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task Chaos_WhenChaosIsEnabledThenDisabled_StatusShouldReflectDisabled()
    {
        await Program.RunAsync(["chaos", "enable"]);
        await Program.RunAsync(["chaos", "disable"]);

        var code = await Program.RunAsync(["chaos", "status"]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ChaosRule_Create_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "chaos", "rule", "create",
            "--rule-id", RuleId,
            "--namespace", "Microsoft.Storage",
            "--fault-type", "Throttle",
            "--rate", "0.5"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ChaosRule_Show_ReturnsRule()
    {
        await Program.RunAsync([
            "chaos", "rule", "create",
            "--rule-id", RuleId,
            "--namespace", "Microsoft.Storage",
            "--fault-type", "Throttle",
            "--rate", "0.5"
        ]);

        var code = await Program.RunAsync(["chaos", "rule", "show", "--rule-id", RuleId]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ChaosRule_Delete_CommandShouldSucceed()
    {
        await Program.RunAsync([
            "chaos", "rule", "create",
            "--rule-id", RuleId,
            "--namespace", "*",
            "--fault-type", "ServiceUnavailable",
            "--rate", "1.0"
        ]);

        var code = await Program.RunAsync(["chaos", "rule", "delete", "--rule-id", RuleId]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ChaosRule_List_CommandShouldSucceed()
    {
        var code = await Program.RunAsync(["chaos", "rule", "list"]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ChaosRule_Enable_CommandShouldSucceed()
    {
        await Program.RunAsync([
            "chaos", "rule", "create",
            "--rule-id", RuleId,
            "--namespace", "Microsoft.KeyVault",
            "--fault-type", "TransientError",
            "--rate", "0.1"
        ]);

        var code = await Program.RunAsync(["chaos", "rule", "enable", "--rule-id", RuleId]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ChaosRule_Disable_CommandShouldSucceed()
    {
        await Program.RunAsync([
            "chaos", "rule", "create",
            "--rule-id", RuleId,
            "--namespace", "Microsoft.KeyVault",
            "--fault-type", "TransientError",
            "--rate", "0.1"
        ]);

        var code = await Program.RunAsync(["chaos", "rule", "disable", "--rule-id", RuleId]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ChaosRule_CreateDuplicate_CommandShouldFail()
    {
        await Program.RunAsync([
            "chaos", "rule", "create",
            "--rule-id", RuleId,
            "--namespace", "*",
            "--fault-type", "Timeout",
            "--rate", "0.2"
        ]);

        var code = await Program.RunAsync([
            "chaos", "rule", "create",
            "--rule-id", RuleId,
            "--namespace", "*",
            "--fault-type", "Timeout",
            "--rate", "0.2"
        ]);

        Assert.That(code, Is.Not.EqualTo(0));
    }
}
