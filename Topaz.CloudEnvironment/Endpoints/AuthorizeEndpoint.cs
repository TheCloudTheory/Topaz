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
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([8899], Protocol.Https);
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if(!context.Request.QueryString.TryGetValueForKey("state", out var state))
        {
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

        var uri = context.Request.QueryString.TryGetValueForKey("redirect_uri", out var redirectUri)
            ? new Uri(redirectUri! + $"?code={code}&state={state}")
            : null;

        if (uri == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }
        
        response.StatusCode = HttpStatusCode.Redirect;
        response.Headers.Location = uri;
    }
}