using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Endpoints;

/// <summary>
/// Implements the Docker Registry V2 ping/challenge endpoint.
/// When an unauthenticated client hits GET /v2/, this returns 401 with a Bearer challenge
/// that directs the client to the OAuth2 token endpoint.
/// </summary>
internal sealed class AcrV2ChallengeEndpoint : IEndpointDefinition
{
    public string[] Endpoints => ["GET /v2/"];

    // Public endpoint — no bearer token required to receive the challenge.
    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var host = context.Request.Host.Value; // e.g. "topazacr06.cr.topaz.local.dev:8892"

        response.StatusCode = HttpStatusCode.Unauthorized;
        response.Headers.Add("Www-Authenticate",
            $"Bearer realm=\"https://{host}/oauth2/token\",service=\"{host}\"");
        response.Content = new StringContent(
            "{\"errors\":[{\"code\":\"UNAUTHORIZED\",\"message\":\"authentication required\"}]}");
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
