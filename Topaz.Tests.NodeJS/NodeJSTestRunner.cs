namespace Topaz.Tests.NodeJS;

/// <summary>
/// One NUnit [Test] method per Node.js smoke script. Each test registers the
/// required hostnames in the Node container then delegates to the script.
///
/// Naming follows the convention used by Topaz.Tests.Python:
/// the test method name describes the service / scenario being tested.
/// </summary>
public class NodeJSTestRunner
{
    [Test]
    public async Task NodeJS_ServiceBusTests()
    {
        await NodeJSHostMapper.EnsureServiceBusHostsMapped("sb-test");
        await NodeJSFixture.RunNodeScript("smoke-service-bus.mjs");
    }

    [Test]
    public async Task NodeJS_EventHubTests()
    {
        await NodeJSHostMapper.EnsureEventHubHostsMapped("test");
        await NodeJSFixture.RunNodeScript("smoke-event-hub.mjs");
    }
}
