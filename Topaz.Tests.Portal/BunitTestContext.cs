using Topaz.Portal.Models.Cli;
using Topaz.Portal.Services;

namespace Topaz.Tests.Portal;

/// <summary>
/// Base class that owns the bUnit <see cref="Bunit.TestContext"/> and disposes it after each test.
/// Inherit from this instead of <see cref="Bunit.TestContext"/> directly so that NUnit lifecycle
/// methods are correctly honoured.
/// </summary>
public abstract class BunitTestContext : Bunit.TestContext
{
    /// <summary>
    /// Registers default NSubstitute stubs for all services that Topaz Portal components inject.
    /// Individual test fixtures can override specific stubs by calling Services.AddSingleton(myMock)
    /// in their own [SetUp] — the last registration wins.
    /// </summary>
    [SetUp]
    public void RegisterDefaultStubs()
    {
        // Accept all JS interop calls (eval-based scrolling, focus, resize) without explicit setup
        JSInterop.Mode = JSRuntimeMode.Loose;

        var suggestionService = Substitute.For<ICliSuggestionService>();
        suggestionService.GetAll().Returns([]);
        suggestionService.GetSuggestions(Arg.Any<string>()).Returns([]);
        Services.AddSingleton(suggestionService);

        Services.AddSingleton(Substitute.For<ITopazClient>());

        var cliExecution = Substitute.For<ICliExecutionService>();
        cliExecution.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CliExecutionResult(string.Empty, false)));
        Services.AddSingleton(cliExecution);
    }

    [TearDown]
    public void TearDown() => Dispose();
}
