using Topaz.CLI;

namespace Topaz.Tests.E2E;

[SetUpFixture]
public class E2EFixture
{
    private static Task? _topaz;
    private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
    
    [OneTimeSetUp]
    public async Task SetUp()
    {
        _topaz = Program.Main([
            "start",
            "--log-level=Information",
            "--skip-dns-registration",
            "--enable-logging-to-file"
        ]);

        await Task.Run(() => _topaz, CancellationTokenSource.Token);
        await Task.Delay(1000);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await CancellationTokenSource.CancelAsync();
    }
}
