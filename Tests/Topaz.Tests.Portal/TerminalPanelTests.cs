using Topaz.Portal.Components.Shared;

namespace Topaz.Tests.Portal;

[TestFixture]
public class TerminalPanel_IsHidden_ByDefault : BunitTestContext
{
    [Test]
    public void Test()
    {
        var cut = Render<TerminalPanel>();

        Assert.That(cut.Markup.Trim(), Is.Empty);
    }
}

[TestFixture]
public class TerminalPanel_IsVisible_AfterOpen : BunitTestContext
{
    [Test]
    public async Task Test()
    {
        var cut = Render<TerminalPanel>();

        await cut.InvokeAsync(() => cut.Instance.Open());
        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".terminal-overlay"), Is.Not.Null));
    }
}

[TestFixture]
public class TerminalPanel_IsHidden_AfterClose : BunitTestContext
{
    [Test]
    public async Task Test()
    {
        var cut = Render<TerminalPanel>();
        await cut.InvokeAsync(() => cut.Instance.Open());
        cut.WaitForAssertion(() => cut.Find(".terminal-overlay"));

        await cut.InvokeAsync(() => cut.Instance.Close());
        cut.WaitForAssertion(() =>
            Assert.That(cut.Markup.Trim(), Is.Empty));
    }
}

[TestFixture]
public class TerminalPanel_Toggle_OpensAndCloses : BunitTestContext
{
    [Test]
    public async Task Test()
    {
        var cut = Render<TerminalPanel>();

        await cut.InvokeAsync(() => cut.Instance.Toggle());
        cut.WaitForAssertion(() => Assert.That(cut.Find(".terminal-overlay"), Is.Not.Null));

        await cut.InvokeAsync(() => cut.Instance.Toggle());
        cut.WaitForAssertion(() => Assert.That(cut.Markup.Trim(), Is.Empty));
    }
}

[TestFixture]
public class TerminalPanel_ClosesViaButton : BunitTestContext
{
    [Test]
    public async Task Test()
    {
        var cut = Render<TerminalPanel>();
        await cut.InvokeAsync(() => cut.Instance.Open());
        cut.WaitForAssertion(() => cut.Find(".terminal-close-btn"));

        cut.Find(".terminal-close-btn").Click();
        cut.WaitForAssertion(() =>
            Assert.That(cut.Markup.Trim(), Is.Empty));
    }
}

[TestFixture]
public class TerminalPanel_ShowsCommandInHistory_AfterEnter : BunitTestContext
{
    [Test]
    public async Task Test()
    {
        var cut = Render<TerminalPanel>();
        await cut.InvokeAsync(() => cut.Instance.Open());
        cut.WaitForAssertion(() => cut.Find(".terminal-input"));

        cut.Find(".terminal-input").Input("topaz version");
        cut.Find(".terminal-input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        cut.WaitForAssertion(() =>
        {
            var commands = cut.FindAll(".terminal-command");
            Assert.That(commands.Any(c => c.TextContent.Contains("topaz version")), Is.True);
        });
    }
}

[TestFixture]
public class TerminalPanel_ClearsInput_AfterEnter : BunitTestContext
{
    [Test]
    public async Task Test()
    {
        var cut = Render<TerminalPanel>();
        await cut.InvokeAsync(() => cut.Instance.Open());
        cut.WaitForAssertion(() => cut.Find(".terminal-input"));

        cut.Find(".terminal-input").Input("topaz version");
        cut.Find(".terminal-input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        cut.WaitForAssertion(() =>
        {
            var input = cut.Find(".terminal-input");
            Assert.That(input.GetAttribute("value") ?? string.Empty, Is.Empty);
        });
    }
}

[TestFixture]
public class TerminalPanel_IgnoresEmptyCommand_OnEnter : BunitTestContext
{
    [Test]
    public async Task Test()
    {
        var cut = Render<TerminalPanel>();
        await cut.InvokeAsync(() => cut.Instance.Open());
        cut.WaitForAssertion(() => cut.Find(".terminal-input"));

        cut.Find(".terminal-input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.That(cut.FindAll(".terminal-command"), Is.Empty);
    }
}

[TestFixture]
public class TerminalPanel_IgnoresNonEnterKey : BunitTestContext
{
    [Test]
    public async Task Test()
    {
        var cut = Render<TerminalPanel>();
        await cut.InvokeAsync(() => cut.Instance.Open());
        cut.WaitForAssertion(() => cut.Find(".terminal-input"));

        cut.Find(".terminal-input").Input("topaz version");
        cut.Find(".terminal-input").KeyDown(new KeyboardEventArgs { Key = "a" });

        Assert.That(cut.FindAll(".terminal-command"), Is.Empty);
    }
}

[TestFixture]
public class TerminalPanel_ExecuteCommand_ShowsOutput : BunitTestContext
{
    [SetUp]
    public void Setup()
    {
        var cliExecution = Substitute.For<ICliExecutionService>();
        cliExecution.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CliExecutionResult("my-resource-group", IsError: false)));
        Services.AddSingleton(cliExecution);
    }

    [Test]
    public async Task Test()
    {
        var cut = Render<TerminalPanel>();
        await cut.InvokeAsync(() => cut.Instance.Open());
        cut.WaitForAssertion(() => cut.Find(".terminal-input"));

        cut.Find(".terminal-input").Input("group list");
        cut.Find(".terminal-input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        cut.WaitForAssertion(() =>
        {
            var results = cut.FindAll(".terminal-result");
            Assert.That(results.Any(r => r.TextContent.Contains("my-resource-group")), Is.True);
        });
    }
}

[TestFixture]
public class TerminalPanel_ExecuteCommand_ShowsError : BunitTestContext
{
    [SetUp]
    public void Setup()
    {
        var cliExecution = Substitute.For<ICliExecutionService>();
        cliExecution.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CliExecutionResult("Resource group not found.", IsError: true)));
        Services.AddSingleton(cliExecution);
    }

    [Test]
    public async Task Test()
    {
        var cut = Render<TerminalPanel>();
        await cut.InvokeAsync(() => cut.Instance.Open());
        cut.WaitForAssertion(() => cut.Find(".terminal-input"));

        cut.Find(".terminal-input").Input("group list");
        cut.Find(".terminal-input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        cut.WaitForAssertion(() =>
        {
            var errorResult = cut.Find(".terminal-result--error");
            Assert.That(errorResult.TextContent, Does.Contain("Resource group not found."));
        });
    }
}

[TestFixture]
public class TerminalPanel_ExecuteCommand_ClearsInputBeforeResult : BunitTestContext
{
    [SetUp]
    public void Setup()
    {
        var cliExecution = Substitute.For<ICliExecutionService>();
        cliExecution.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CliExecutionResult("ok", IsError: false)));
        Services.AddSingleton(cliExecution);
    }

    [Test]
    public async Task Test()
    {
        var cut = Render<TerminalPanel>();
        await cut.InvokeAsync(() => cut.Instance.Open());
        cut.WaitForAssertion(() => cut.Find(".terminal-input"));

        cut.Find(".terminal-input").Input("group list");
        cut.Find(".terminal-input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        cut.WaitForAssertion(() =>
        {
            var input = cut.Find(".terminal-input");
            Assert.That(input.GetAttribute("value") ?? string.Empty, Is.Empty);
        });
    }
}
