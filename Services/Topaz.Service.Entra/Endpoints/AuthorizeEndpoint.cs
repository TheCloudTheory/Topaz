using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Entra.Endpoints;

public class AuthorizeEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    internal static readonly ConcurrentDictionary<string, string> Nonces = new();

    /// <summary>Maps an authorization code to the login_hint (username/UPN) supplied during the authorize request.</summary>
    internal static readonly ConcurrentDictionary<string, string> AuthCodes = new();

    public string[] Endpoints =>
    [
        "GET /organizations/oauth2/v2.0/authorize",
    ];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!context.Request.Query.TryGetValueForKey("state", out var state))
        {
            logger.LogWarning("Missing state parameter in authorize request.");
            
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        // Generate auth code and store nonce (if provided) so token exchange can include it.
        var code = $"Topaz{Guid.NewGuid():N}";
        context.Request.Query.TryGetValueForKey("nonce", out var nonce);
        if (!string.IsNullOrEmpty(nonce))
        {
            Nonces[code] = nonce;
        }

        // Store login_hint so the token endpoint can resolve the correct user identity.
        context.Request.Query.TryGetValueForKey("login_hint", out var loginHint);
        if (!string.IsNullOrEmpty(loginHint))
        {
            AuthCodes[code] = loginHint;
        }
        
        logger.LogDebug(nameof(AuthorizeEndpoint), nameof(GetResponse), "Generated auth code: {0}", code);

        if (!context.Request.Query.TryGetValueForKey("redirect_uri", out var redirectUri))
        {
            logger.LogWarning("Missing redirect_uri parameter in authorize request.");

            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        context.Request.Query.TryGetValueForKey("response_mode", out var responseMode);

        if (string.Equals(responseMode, "form_post", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug(nameof(AuthorizeEndpoint), nameof(GetResponse), "Using form_post response mode for redirect_uri: {0}", redirectUri);

            var html = $"""
                <!DOCTYPE html>
                <html>
                <head><title>Redirecting...</title></head>
                <body>
                <form method="POST" action="{WebUtility.HtmlEncode(redirectUri)}">
                  <input type="hidden" name="code" value="{WebUtility.HtmlEncode(code)}" />
                  <input type="hidden" name="state" value="{WebUtility.HtmlEncode(state!)}" />
                </form>
                <script>document.forms[0].submit();</script>
                </body>
                </html>
                """;

            response.Content = new StringContent(html, Encoding.UTF8, "text/html");
            response.StatusCode = HttpStatusCode.OK;
        }
        else
        {
            var uri = new Uri(redirectUri! + $"?code={code}&state={state}");
            logger.LogDebug(nameof(AuthorizeEndpoint), nameof(GetResponse), "Redirecting to: {0}", uri);
            response.StatusCode = HttpStatusCode.Redirect;
            response.Headers.Location = uri;
        }
    }
}