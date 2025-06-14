using Topaz.CLI;

namespace Topaz.Tests.E2E;

[SetUpFixture]
public class E2EFixture
{
    [OneTimeSetUp]
    public async Task SetUp()
    {
        await Program.Main([
                "start",
                "--log-level=Information",
            ]);

        await Task.Delay(1000);
    }
}
