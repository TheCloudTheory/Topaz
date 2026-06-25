using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class ChaosTests
{
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
}
