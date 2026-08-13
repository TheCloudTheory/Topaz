using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class ContextSwitchCommandTests
{
    [Test]
    public async Task ContextSwitch_WithName_ReturnsSuccess()
    {
        var exitCode = await Program.RunAsync(["context", "switch", "--name", "TestCloud"]);

        Assert.That(exitCode, Is.Zero);
    }

    [Test]
    public async Task ContextSwitch_WithUseTopaz_ReturnsSuccess()
    {
        var exitCode = await Program.RunAsync(["context", "switch", "--use-topaz"]);

        Assert.That(exitCode, Is.Zero);
    }

    [Test]
    public async Task ContextSwitch_WithUseDefault_ReturnsSuccess()
    {
        var exitCode = await Program.RunAsync(["context", "switch", "--use-default"]);

        Assert.That(exitCode, Is.Zero);
    }

    [Test]
    public async Task ContextSwitch_WithShortNameFlag_ReturnsSuccess()
    {
        var exitCode = await Program.RunAsync(["context", "switch", "-n", "TestCloud"]);

        Assert.That(exitCode, Is.Zero);
    }
}
