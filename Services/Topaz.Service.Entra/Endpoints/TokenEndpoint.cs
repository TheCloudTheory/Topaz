using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Topaz.Identity;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Models.Responses;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Entra.Endpoints;

public class TokenEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private const string Issuer = "https://topaz.local.dev:8899/organizations/v2.0";

    private readonly UserDataPlane _userDataPlane = UserDataPlane.New(logger);

    public string[] Endpoints =>
    [
        "POST /organizations/oauth2/v2.0/token",
    ];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        // Build a minimal unsigned JWT (header.payload.) so MSAL can decode the id_token payload.

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

            var userOperation = _userDataPlane.Get(UserIdentifier.From(username));
            if (userOperation.Resource == null || userOperation.Result != OperationResult.Success)
            {
                response.CreateJsonContentResponse(ErrorResponse.Create(ErrorResponse.InvalidClient, "Invalid user."),
                    HttpStatusCode.BadRequest);
                return;
            }

            var savedPassword = userOperation.Resource.PasswordProfile?.Password;
            var password = ExtractValueForToken(context, form, "password", null);

            if (savedPassword == null || !string.Equals(savedPassword, password, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError(nameof(TokenEndpoint), nameof(GetResponse), "Invalid password.");
                response.CreateJsonContentResponse(
                    ErrorResponse.Create(ErrorResponse.InvalidClient, "Invalid password."),
                    HttpStatusCode.BadRequest);
                return;
            }

            objectId = userOperation.Resource.Id;
        }

        switch (grantType)
        {
            case "refresh_token":
            {
                logger.LogDebug(nameof(TokenEndpoint), nameof(GetResponse),
                    "Extracting object ID from refresh token...");

                var refreshToken = ExtractValueForToken(context, form, "refresh_token", null);
                if (refreshToken == null)
                {
                    logger.LogError(nameof(TokenEndpoint), nameof(GetResponse), "Could not extract refresh token.");
                    response.CreateJsonContentResponse(
                        ErrorResponse.Create(ErrorResponse.InvalidRequest, "Could not extract refresh token."),
                        HttpStatusCode.BadRequest);
                    return;
                }

                try
                {
                    var validatedToken = JwtHelper.ValidateJwt(refreshToken);
                    if (validatedToken == null)
                    {
                        logger.LogError(nameof(TokenEndpoint), nameof(GetResponse), "Could not validate refresh token.");
                        response.CreateJsonContentResponse(
                            ErrorResponse.Create(ErrorResponse.InvalidClient, "Could not validate refresh token."),
                            HttpStatusCode.Unauthorized);
                        return;
                    }

                    objectId = validatedToken.Subject;
                }
                catch (SecurityTokenExpiredException ex)
                {
                    logger.LogError(nameof(TokenEndpoint), nameof(GetResponse), $"Refresh token expired - {ex.Message}");
                    response.CreateJsonContentResponse(
                        ErrorResponse.Create(ErrorResponse.InvalidRequest, "Refresh token expired"),
                        HttpStatusCode.BadRequest);
                }
                
                break;
            }
            case "authorization_code":
            {
                logger.LogDebug(nameof(TokenEndpoint), nameof(GetResponse),
                    "Extracting object ID from authorization code...");

                // For now if authorization code is provided, we're authenticating the user as a global admin
                objectId = Globals.GlobalAdminId;
                
                var userOperation = _userDataPlane.Get(UserIdentifier.From(objectId));
                if (userOperation.Resource == null || userOperation.Result != OperationResult.Success)
                {
                    logger.LogError(nameof(TokenEndpoint), nameof(GetResponse), "Could not find user.");
                    response.CreateJsonContentResponse(
                        ErrorResponse.Create(ErrorResponse.InvalidClient, "Could not find user."),
                        HttpStatusCode.BadRequest);
                    return;
                }
                
                username = userOperation.Resource.UserPrincipalName;
                
                break;
            }
            case "client_credentials":
            {
                // For the emulator, any client_credentials request is accepted and mapped to the
                // global admin identity so that all subsequent ARM/Graph API calls succeed without
                // requiring the service principal to be pre-registered or have role assignments.
                logger.LogDebug(nameof(TokenEndpoint), nameof(GetResponse),
                    "client_credentials grant — accepting any credentials as global admin.");

                objectId = Globals.GlobalAdminId;
                break;
            }
        }

        if (objectId == null)
        {
            logger.LogError(nameof(TokenEndpoint), nameof(GetResponse), "Could not extract object ID from request.");
            response.CreateJsonContentResponse(
                ErrorResponse.Create(ErrorResponse.InvalidRequest, "Could not extract object ID from request."),
                HttpStatusCode.BadRequest);
            return;
        }

        var token = CreateTokenResponse(context, objectId, clientId, storedNonce, username, form);

        response.CreateJsonContentResponse(token);
    }

    private static TokenResponse CreateTokenResponse(HttpContext context, string objectId, string? clientId,
        string? storedNonce,
        string? username, Dictionary<string, string> form)
    {
        {
            var token = new TokenResponse
            {
                AccessToken = new AzureLocalCredential(objectId)
                    .GetToken(new TokenRequestContext(), CancellationToken.None).Token,
                RefreshToken = new AzureLocalCredential(objectId)
                    .GetToken(new TokenRequestContext(), CancellationToken.None).Token,
                IdToken = CreateIdToken(Issuer, clientId!, storedNonce, username ?? objectId, objectId,
                    EntraService.TenantId),
                Scope = form.TryGetValue("scope", out var scope)
                    ? scope
                    : context.Request.QueryString.TryGetValueForKey("scope", out var qscope)
                        ? qscope
                        : "openid profile offline_access"
            };
            return token;
        }

        static string Base64UrlEncode(string input)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(input))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        static string CreateIdToken(string issuer, string audience, string? nonce, string userName, string objectId,
            string tenantId)
        {
            var header = new { alg = "none", typ = "JWT" };
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var payload = new Dictionary<string, object>
            {
                { "iss", issuer },
                { "aud", audience },
                { "iat", now },
                { "exp", now + 3600 },
                { "oid", objectId },
                { "tid", tenantId },
                { "sub", objectId },
                { "preferred_username", userName }
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