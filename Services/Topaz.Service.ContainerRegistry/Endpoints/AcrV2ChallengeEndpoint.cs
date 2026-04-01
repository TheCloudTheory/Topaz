using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Topaz.Identity;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Endpoints;

/// <summary>
/// Implements the Docker Registry V2 ping endpoint.
/// - No Authorization header → 401 with Bearer challenge (directs client to /oauth2/token).
/// - Basic Authorization → validate credentials; return 200 on success.
/// - Bearer Authorization → validate JWT; return 200 on success.
/// </summary>
internal sealed class AcrV2ChallengeEndpoint : IEndpointDefinition
{
    public string[] Endpoints => ["GET /v2/"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var host = context.Request.Host.Value;
        var authorization = context.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrWhiteSpace(authorization))
        {
            ReturnChallenge(response, host);
            return;
        }

        if (authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            var encoded = authorization["Basic ".Length..].Trim();
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var colon = decoded.IndexOf(':');
            var username = colon > 0 ? decoded[..colon] : decoded;
            var password = colon > 0 ? decoded[(colon + 1)..] : string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ReturnChallenge(response, host);
                return;
            }

            // Emulator: any non-empty Basic credentials are accepted.
            ReturnOk(response);
            return;
        }

        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authorization["Bearer ".Length..].Trim();
            var validated = JwtHelper.ValidateJwt(token);
            if (validated == null)
            {
                ReturnChallenge(response, host);
                return;
            }

            ReturnOk(response);
            return;
        }

        ReturnChallenge(response, host);
    }

    private static void ReturnOk(HttpResponseMessage response)
    {
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent("{}");
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }

    private static void ReturnChallenge(HttpResponseMessage response, string host)
    {
        response.StatusCode = HttpStatusCode.Unauthorized;
        response.Headers.Add("Www-Authenticate",
            $"Bearer realm=\"https://{host}/oauth2/token\",service=\"{host}\"");
        response.Content = new StringContent(
            "{\"errors\":[{\"code\":\"UNAUTHORIZED\",\"message\":\"authentication required\"}]}");
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
