using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Topaz.CloudEnvironment.Models.Responses;
using Topaz.Identity;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.CloudEnvironment.Endpoints;

public class TokenEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private const string Issuer = "https://topaz.local.dev:8899/organizations/v2.0";
    
    private readonly UserDataPlane _dataPlane = UserDataPlane.New(logger);
    
    public string[] Endpoints =>
    [
        "POST /organizations/oauth2/v2.0/token",
    ];

    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        // Build a minimal unsigned JWT (header.payload.) so MSAL can decode the id_token payload.
        static string Base64UrlEncode(string input)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(input))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        // Parse POST form body to obtain the authorization 'code'.
        string body;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            body = reader.ReadToEnd();
        }

        logger.LogDebug(nameof(TokenEndpoint), nameof(GetResponse), "Received body: {0}", body);

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

        logger.LogDebug(nameof(TokenEndpoint), nameof(GetResponse), "Received code: {0}", code);

        var clientId = ExtractValueForToken(context, form, "client_id", Guid.NewGuid().ToString());
        var username = ExtractValueForToken(context, form, "username", null);
        var grantType = ExtractValueForToken(context, form, "grant_type", null);

        logger.LogDebug(nameof(TokenEndpoint), nameof(GetResponse), "Received client_id: {0}", clientId);
        logger.LogDebug(nameof(TokenEndpoint), nameof(GetResponse), "Received username: {0}", username);
        logger.LogDebug(nameof(TokenEndpoint), nameof(GetResponse), "Received grant type: {0}", grantType);

        // Look up and remove stored nonce for this code if present.
        AuthorizeEndpoint.Nonces.TryRemove(code ?? string.Empty, out var storedNonce);

        // If username isn't empty we can look for the user to check their ID
        string? objectId = null;
        if (!string.IsNullOrEmpty(username) && grantType == "password")
        {
            logger.LogDebug(nameof(TokenEndpoint), nameof(GetResponse), "Looking up user `{0}`...", username);
            
            var userOperation = _dataPlane.Get(UserIdentifier.From(username));
            if (userOperation.Resource == null || userOperation.Result != OperationResult.Success)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }
            
            objectId = userOperation.Resource.Id;
        }

        if (grantType == "refresh_token")
        {
            logger.LogDebug(nameof(TokenEndpoint), nameof(GetResponse), "Extracting object ID from refresh token...");
            
            var refreshToken = ExtractValueForToken(context, form, "refresh_token", null);
            if (refreshToken == null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }
            
            var validatedToken = JwtHelper.ValidateJwt(refreshToken);
            if (validatedToken == null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            objectId = validatedToken.Subject;
        }

        if (objectId == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var token = new TokenResponse
        {
            AccessToken = new AzureLocalCredential(objectId!)
                .GetToken(new TokenRequestContext(), CancellationToken.None).Token,
            RefreshToken = new AzureLocalCredential(objectId!)
                .GetToken(new TokenRequestContext(), CancellationToken.None).Token,
            IdToken = CreateIdToken(Issuer, clientId!, storedNonce),
            Scope = form.TryGetValue("scope", out var scope)
                ? scope
                : (context.Request.QueryString.TryGetValueForKey("scope", out var qscope)
                    ? qscope
                    : "openid profile offline_access")
        };

        response.Content = new StringContent(token.ToString());
        response.StatusCode = HttpStatusCode.OK;
        return;

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
                payload["nonce"] = nonce;
            }

            var headerJson = JsonSerializer.Serialize(header);
            var payloadJson = JsonSerializer.Serialize(payload);

            return Base64UrlEncode(headerJson) + "." + Base64UrlEncode(payloadJson) + ".";
        }
    }

    private static string? ExtractValueForToken(HttpContext context, Dictionary<string, string> form, string key,
        string? defaultValue)
    {
        return form.TryGetValue(key, out var cid) ? cid :
            context.Request.QueryString.TryGetValueForKey(key, out var qcid) ? qcid :
            defaultValue;
    }
}