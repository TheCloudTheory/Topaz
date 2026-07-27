using NUnit.Framework;
using Topaz.MCP.Tools;
using Topaz.Shared;

namespace Topaz.Tests.MCP;

[TestFixture]
public class StatusToolTests
{
    [Test]
    public async Task GetTopazStatus_ReturnsVersion()
    {
        var result = await StatusTool.GetTopazStatus();

        Assert.That(result.Version, Is.Not.Null.And.Not.Empty.And.Not.EqualTo("Unknown"));
    }

    [Test]
    public async Task GetTopazStatus_ResourceManagerServiceIsUp()
    {
        var result = await StatusTool.GetTopazStatus();

        var rmService = result.Services.Single(s => s.Port == GlobalSettings.DefaultResourceManagerPort);

        Assert.That(rmService.IsUp, Is.True);
    }

    [Test]
    public async Task GetTopazStatus_ReturnsAllKnownServices()
    {
        var result = await StatusTool.GetTopazStatus();

        Assert.That(result.Services, Has.Count.EqualTo(15));
    }

    [Test]
    public async Task GetTopazStatus_ConnectProxyServiceIsPresent()
    {
        var result = await StatusTool.GetTopazStatus();

        Assert.That(result.Services.Any(s => s.Port == GlobalSettings.ConnectProxyPort), Is.True);
    }

    [Test]
    public async Task GetTopazStatus_AppServiceForwardProxyIsPresent()
    {
        var result = await StatusTool.GetTopazStatus();

        Assert.That(result.Services.Any(s => s.Name == "App Service Forward Proxy"), Is.True);
    }

    [Test]
    public async Task GetTopazStatus_ReturnsChaosEnabled()
    {
        var result = await StatusTool.GetTopazStatus();

        // Chaos is disabled by default; just assert the field is present and readable.
        Assert.That(result.ChaosEnabled, Is.False);
    }
}
