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

        Assert.That(result.Services, Has.Count.EqualTo(11));
    }
}
