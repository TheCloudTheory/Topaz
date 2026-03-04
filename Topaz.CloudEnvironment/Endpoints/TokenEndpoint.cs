using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Topaz.CloudEnvironment.Models.Responses;
using Topaz.Identity;
using Topaz.Service.Entra;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Models;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.CloudEnvironment.Endpoints;

public class TokenEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private const string Issuer = "https://topaz.local.dev:8899/organizations/v2.0";

    private readonly UserDataPlane _userDataPlane = UserDataPlane.New(logger);
    private readonly ApplicationsDataPlane _applicationsDataPlane = ApplicationsDataPlane.New(logger);

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

            var userOperation = _userDataPlane.Get(UserIdentifier.From(username));
            if (userOperation.Resource == null || userOperation.Result != OperationResult.Success)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
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
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return;
                }

                var validatedToken = JwtHelper.ValidateJwt(refreshToken);
                if (validatedToken == null)
                {
                    logger.LogError(nameof(TokenEndpoint), nameof(GetResponse), "Could not validate refresh token.");
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return;
                }

                objectId = validatedToken.Subject;
                break;
            }
            case "client_credentials":
            {
                logger.LogDebug(nameof(TokenEndpoint), nameof(GetResponse),
                    "Extracting app ID and client secret from client credentials...");

                var clientSecret = ExtractValueForToken(context, form, "client_secret", null);
                if (clientSecret == null)
                {
                    logger.LogError(nameof(TokenEndpoint), nameof(GetResponse), "Could not extract client secret.");
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return;
                }

                var appId = ExtractValueForToken(context, form, "client_id", null);
                if (appId == null)
                {
                    logger.LogError(nameof(TokenEndpoint), nameof(GetResponse), "Could not extract app ID.");
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return;
                }

                var appOperation = _applicationsDataPlane.Get(ApplicationIdentifier.From(appId), true);
                if (appOperation.Resource == null || appOperation.Result != OperationResult.Success)
                {
                    logger.LogError(nameof(TokenEndpoint), nameof(GetResponse), "Could not find app.");
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return;
                }

                var applicationCredentials =
                    FindCredentialsForClientSecret(appOperation.Resource.PasswordCredentials!, clientSecret);

                if (!applicationCredentials)
                {
                    logger.LogError(nameof(TokenEndpoint), nameof(GetResponse), "Invalid client secret.");
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return;
                }

                objectId = appOperation.Resource.Id;
                break;
            }
        }

        if (objectId == null)
        {
            logger.LogError(nameof(TokenEndpoint), nameof(GetResponse), "Could not extract object ID from request.");
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var token = new TokenResponse
        {
            AccessToken = new AzureLocalCredential(objectId!)
                .GetToken(new TokenRequestContext(), CancellationToken.None).Token,
            RefreshToken = new AzureLocalCredential(objectId!)
                .GetToken(new TokenRequestContext(), CancellationToken.None).Token,
            IdToken = CreateIdToken(Issuer, clientId!, storedNonce, username ?? objectId, objectId,
                EntraService.TenantId),
            Scope = form.TryGetValue("scope", out var scope)
                ? scope
                : (context.Request.QueryString.TryGetValueForKey("scope", out var qscope)
                    ? qscope
                    : "openid profile offline_access")
        };

        response.CreateJsonContentResponse(token);
        return;

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

    private bool FindCredentialsForClientSecret(Application.PasswordCredentialData[] resourcePasswordCredentials,
        string clientSecret)
    {
        logger.LogDebug(nameof(TokenEndpoint), nameof(FindCredentialsForClientSecret),
            "Finding credentials for client secret: {0}", clientSecret);

        foreach (var credential in resourcePasswordCredentials)
        {
            logger.LogDebug(nameof(TokenEndpoint), nameof(FindCredentialsForClientSecret),
                "Checking credential: {0}", credential);

            var storedCredentials = credential.SecretText;
            if (storedCredentials == null)
            {
                logger.LogDebug(nameof(TokenEndpoint), nameof(FindCredentialsForClientSecret),
                    "Credential secret text is null for credential: {0}", credential);
                continue;
            }

            logger.LogDebug(nameof(TokenEndpoint), nameof(FindCredentialsForClientSecret),
                "Checking credential: {0} against {1}", credential, clientSecret);

            if (string.Equals(storedCredentials, clientSecret, StringComparison.OrdinalIgnoreCase))
                return true;

            logger.LogDebug(nameof(TokenEndpoint), nameof(FindCredentialsForClientSecret),
                "Match for raw credentials failed, try with encoded one.");

            var encodedCredentials = Uri.EscapeDataString(storedCredentials);
            logger.LogDebug(nameof(TokenEndpoint), nameof(FindCredentialsForClientSecret),
                "Checking credential: {0} (encoded: {1})", credential, encodedCredentials);

            if (!string.Equals(encodedCredentials, clientSecret, StringComparison.OrdinalIgnoreCase)) continue;
            logger.LogDebug(nameof(TokenEndpoint), nameof(FindCredentialsForClientSecret),
                "Credential found for client secret: {0}", clientSecret);

            return true;
        }

        logger.LogDebug(nameof(TokenEndpoint), nameof(FindCredentialsForClientSecret),
            "No matching credential found for client secret: {0}", clientSecret);
        return false;
    }

    private static string? ExtractValueForToken(HttpContext context, Dictionary<string, string> form, string key,
        string? defaultValue)
    {
        return form.TryGetValue(key, out var cid) ? cid :
            context.Request.QueryString.TryGetValueForKey(key, out var qcid) ? qcid :
            defaultValue;
    }
}