using System.Net;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Host;
using Topaz.Identity;
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
        var router = new Router(new Pipeline(logger), new GlobalOptions(), logger);
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var endpoints = new IEndpointDefinition[]
        {
            new GetNamespaceEndpoint(logger),
            new ServiceBusServiceEndpoint(logger)
        };
        var context = new DefaultHttpContext()
        {
            Request =
            {
                Method = "GET",
                Path = $"/subscriptions/{Guid.Empty}/resourceGroups/rg/providers/Microsoft.ServiceBus/namespaces/ns",
                Host = HostString.FromUriComponent($"localhost:{GlobalSettings.DefaultResourceManagerPort}"),
            }
        };
        context.Request.Headers.Add("Authorization",
            $"Bearer {(await credentials.GetTokenAsync(new TokenRequestContext(), CancellationToken.None)).Token}");

        // Act
        await router.MatchAndExecuteEndpoint(endpoints, context);

        // Assert
        Assert.That(context.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
    }
}