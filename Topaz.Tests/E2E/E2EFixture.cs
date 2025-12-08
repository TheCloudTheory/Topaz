using Topaz.CLI;

namespace Topaz.Tests.E2E;

[SetUpFixture]
public class E2EFixture
{
    private static Task? _topaz;
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    
    [OneTimeSetUp]
    public async Task SetUp()
    {
        _topaz = Program.Main([
            "start",
            "--log-level=Debug",
            "--skip-dns-registration",
            "--enable-logging-to-file",
            "--refresh-log",
            "--emulator-ip-address",
            "127.0.0.1"
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
