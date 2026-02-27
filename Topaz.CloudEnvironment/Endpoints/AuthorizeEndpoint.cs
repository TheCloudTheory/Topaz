using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.CloudEnvironment.Endpoints;

public class AuthorizeEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    internal static readonly ConcurrentDictionary<string, string> Nonces = new();

    public string[] Endpoints =>
    [
        "GET /organizations/oauth2/v2.0/authorize",
    ];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!context.Request.QueryString.TryGetValueForKey("state", out var state))
        {
            logger.LogWarning("Missing state parameter in authorize request.");
            
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        // Generate auth code and store nonce (if provided) so token exchange can include it.
        var code = $"Topaz{Guid.NewGuid():N}";
        context.Request.QueryString.TryGetValueForKey("nonce", out var nonce);
        if (!string.IsNullOrEmpty(nonce))
        {
            Nonces[code] = nonce;
        }
        
        logger.LogDebug(nameof(AuthorizeEndpoint), nameof(GetResponse), "Generated auth code: {0}", code);

        var uri = context.Request.QueryString.TryGetValueForKey("redirect_uri", out var redirectUri)
            ? new Uri(redirectUri! + $"?code={code}&state={state}")
            : null;
        
        logger.LogDebug(nameof(AuthorizeEndpoint), nameof(GetResponse), "Redirecting to: {0}", uri);

        if (uri == null)
        {
            logger.LogWarning("Missing redirect_uri parameter in authorize request.");
            
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        response.StatusCode = HttpStatusCode.Redirect;
        response.Headers.Location = uri;
    }
}