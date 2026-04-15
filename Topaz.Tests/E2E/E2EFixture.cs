using TopazHost = Topaz.Host.Host;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Tests;

[SetUpFixture]
public class E2EFixture
{
    private static Task? _topaz;
    private static readonly CancellationTokenSource CancellationTokenSource = new();

    [OneTimeSetUp]
    public async Task SetUp()
    {
        var logger = new PrettyTopazLogger();
        logger.SetLoggingLevel(LogLevel.Debug);
        logger.EnableLoggingToFile(true);

        var host = new TopazHost(new GlobalOptions
        {
            EnableLoggingToFile = true,
            EmulatorIpAddress = "127.0.0.1"
        }, logger);

        _topaz = host.StartAsync(CancellationTokenSource.Token);

        await Task.Delay(1000);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await CancellationTokenSource.CancelAsync();

        if (_topaz != null)
        {
            try
            {
                await _topaz.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is triggered
            }
            catch (TimeoutException)
            {
                // Task didn't complete in time
            }
        }

        CancellationTokenSource.Dispose();
    }
}
