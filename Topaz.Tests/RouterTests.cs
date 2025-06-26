using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Host;
using Topaz.Service.EventHub.Endpoints;
using Topaz.Service.ServiceBus.Endpoints;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Tests;

public class RouterTests
{
    [Test]
    public async Task RouterTests_WhenSimilarEndpointsAreAvailable_TheCorrectOneShouldBeSelected()
    {
        // Arrange
        var logger = new PrettyTopazLogger();
        var router = new Router(new GlobalOptions(), logger);
        var endpoints = new IEndpointDefinition[]
        {
            new EventHubServiceEndpoint(logger),
            new ServiceBusServiceEndpoint(logger)
        };
        var context = new DefaultHttpContext()
        {
            Request =
            {
                Method = "GET",
                Path = $"/subscriptions/{Guid.Empty}/resourceGroups/rg/providers/Microsoft.ServiceBus/namespaces/ns",
                Host = HostString.FromUriComponent($"localhost:{GlobalSettings.DefaultResourceManagerPort}")
            }
        };

        // Act
        await router.MatchAndExecuteEndpoint(endpoints, context);

        // Assert
        Assert.That(context.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
    }
}