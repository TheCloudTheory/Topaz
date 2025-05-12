namespace Topaz.Tests.E2E;

[SetUpFixture]
public class E2EFixture
{
    [OneTimeSetUp]
    public async Task SetUp()
    {
        await Program.Main([
                "start"
            ]);

        await Task.Delay(1000);
    }
}
