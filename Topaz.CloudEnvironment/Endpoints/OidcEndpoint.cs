using System.Net;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using Topaz.Service.Shared;
using Topaz.CloudEnvironment.Models.Responses;
using Topaz.Shared.Extensions;

namespace Topaz.CloudEnvironment.Endpoints;

internal sealed class OidcEndpoint : IEndpointDefinition
{
    private static readonly ConcurrentDictionary<string, string> Nonces = new();
    
    public string[] Endpoints =>
    [
        "GET /organizations/v2.0/.well-known/openid-configuration",
        "GET /organizations/oauth2/v2.0/authorize",
        "GET /{tenantId}/v2.0/.well-known/openid-configuration",
        "POST /organizations/oauth2/v2.0/token",
    ];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([8899], Protocol.Https);

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query,
        GlobalOptions options)
    {
        var response = new HttpResponseMessage();

        if (path.EndsWith("/authorize"))
        {
            HandleAuthorizeRequest(response, query);
        }
        else if (path.EndsWith("/token"))
        {
            HandleTokenRequest(response, input, query);
        }
        else
        {
            var config = new OpenIdConfigurationResponse();
            response.Content = new StringContent(config.ToString());
        }

        return response;
    }
    
    private void HandleTokenRequest(HttpResponseMessage response, Stream input, QueryString query)
    {
        // Build a minimal unsigned JWT (header.payload.) so MSAL can decode the id_token payload.
        static string Base64UrlEncode(string input)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(input))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        static string CreateIdToken(string issuer, string audience, string? nonce)
        {
            var header = new { alg = "none", typ = "JWT" };
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var payload = new Dictionary<string, object>
            {
                { "iss", issuer },
                { "aud", audience },
                { "iat", now },
                { "exp", now + 3600 },
                { "oid", Guid.NewGuid().ToString() },
                { "tid", "TopazTenant" },
                { "sub", Guid.NewGuid().ToString() },
                { "preferred_username", "topaz@local" }
            };

            if (!string.IsNullOrEmpty(nonce))
            {
                payload["nonce"] = nonce!;
            }

            var headerJson = JsonSerializer.Serialize(header);
            var payloadJson = JsonSerializer.Serialize(payload);

            return Base64UrlEncode(headerJson) + "." + Base64UrlEncode(payloadJson) + ".";
        }

        // Parse POST form body to obtain the authorization 'code'.
        string body;
        using (var reader = new StreamReader(input, Encoding.UTF8, leaveOpen: true))
        {
            body = reader.ReadToEnd();
        }

        string? code = null;
        var form = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(body))
        {
            foreach (var part in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = part.IndexOf('=');
                if (idx <= 0) continue;
                
                var k = Uri.UnescapeDataString(part[..idx]);
                var v = Uri.UnescapeDataString(part[(idx + 1)..]);
                form[k] = v;
            }
            form.TryGetValue("code", out code);
        }

        var clientId = form.TryGetValue("client_id", out var cid) ? cid : query.TryGetValueForKey("client_id", out var qcid) ? qcid : "04b07795-8ddb-461a-bbee-02f9e1bf7b46";
        var issuer = "https://topaz.local.dev:8899/organizations/v2.0";

        // Look up and remove stored nonce for this code if present.
        Nonces.TryRemove(code ?? string.Empty, out var storedNonce);

        var token = new TokenResponse
        {
            AccessToken = "TopazAccessToken" + Guid.NewGuid().ToString("N"),
            RefreshToken = "TopazRefreshToken" + Guid.NewGuid().ToString("N"),
            IdToken = CreateIdToken(issuer, clientId, storedNonce),
            Scope = form.TryGetValue("scope", out var scope) ? scope : (query.TryGetValueForKey("scope", out var qscope) ? qscope : "openid profile offline_access")
        };

        response.Content = new StringContent(token.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleAuthorizeRequest(HttpResponseMessage response, QueryString query)
    {
        if(!query.TryGetValueForKey("state", out var state))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }
        // Generate auth code and store nonce (if provided) so token exchange can include it.
        var code = $"Topaz{Guid.NewGuid():N}";
        query.TryGetValueForKey("nonce", out var nonce);
        if (!string.IsNullOrEmpty(nonce))
        {
            Nonces[code] = nonce;
        }

        var uri = query.TryGetValueForKey("redirect_uri", out var redirectUri)
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