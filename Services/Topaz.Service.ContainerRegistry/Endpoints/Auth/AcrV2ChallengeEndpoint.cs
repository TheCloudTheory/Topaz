using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Identity;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Endpoints.Auth;

/// <summary>
/// Implements the Docker Registry V2 ping endpoint.
/// - No Authorization header → 401 with Bearer challenge (directs client to /oauth2/token).
/// - Basic Authorization → only accepted when admin user is enabled; validates against stored admin credentials.
/// - Bearer Authorization → validate JWT; return 200 on success.
/// </summary>
internal sealed class AcrV2ChallengeEndpoint(ContainerRegistryControlPlane controlPlane, ITopazLogger logger) : IEndpointDefinition
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
            logger.LogDebug(nameof(AcrV2ChallengeEndpoint), nameof(GetResponse), "No Authorization header — returning challenge.");
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
                logger.LogDebug(nameof(AcrV2ChallengeEndpoint), nameof(GetResponse), "Basic auth — missing username or password.");
                ReturnChallenge(response, host);
                return;
            }

            var registry = ResolveRegistry(context);
            if (registry == null || !registry.Properties.AdminUserEnabled)
            {
                logger.LogDebug(nameof(AcrV2ChallengeEndpoint), nameof(GetResponse),
                    "Basic auth — registry not found or admin user disabled for '{0}'.", username);
                ReturnChallenge(response, host);
                return;
            }

            if (!string.Equals(registry.Properties.AdminUsername, username, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(registry.Properties.AdminPassword, password, StringComparison.Ordinal))
            {
                logger.LogDebug(nameof(AcrV2ChallengeEndpoint), nameof(GetResponse),
                    "Basic auth — invalid credentials for user '{0}'.", username);
                ReturnChallenge(response, host);
                return;
            }

            logger.LogDebug(nameof(AcrV2ChallengeEndpoint), nameof(GetResponse),
                "Basic auth — credentials accepted for user '{0}'.", username);
            ReturnOk(response);
            return;
        }

        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authorization["Bearer ".Length..].Trim();
            var validated = JwtHelper.ValidateJwt(token);
            if (validated == null)
            {
                logger.LogDebug(nameof(AcrV2ChallengeEndpoint), nameof(GetResponse), "Bearer auth — invalid or expired JWT.");
                ReturnChallenge(response, host);
                return;
            }

            logger.LogDebug(nameof(AcrV2ChallengeEndpoint), nameof(GetResponse),
                "Bearer auth — valid JWT for subject '{0}'.", validated.Subject);
            ReturnOk(response);
            return;
        }

        logger.LogDebug(nameof(AcrV2ChallengeEndpoint), nameof(GetResponse),
            "Unrecognised Authorization scheme — returning challenge.");
        ReturnChallenge(response, host);
    }

    private ContainerRegistryResource? ResolveRegistry(HttpContext context)
    {
        var hostName = context.Request.Host.Host; // e.g. "topazacr06.cr.topaz.local.dev"
        var registryName = hostName.Split('.')[0];

        var identifiers = GlobalDnsEntries.GetEntry(ContainerRegistryService.UniqueName, registryName);
        if (identifiers == null) return null;

        var operation = controlPlane.Get(
            SubscriptionIdentifier.From(identifiers.Value.subscription),
            ResourceGroupIdentifier.From(identifiers.Value.resourceGroup!),
            registryName);

        return operation.Resource;
    }

    private static void ReturnOk(HttpResponseMessage response)
    {
        response.CreateJsonContentResponse("{}");
    }

    private static void ReturnChallenge(HttpResponseMessage response, string host)
    {
        response.Headers.Add("Www-Authenticate",
            $"Bearer realm=\"https://{host}/oauth2/token\",service=\"{host}\"");
        response.CreateJsonContentResponse(
            "{\"errors\":[{\"code\":\"UNAUTHORIZED\",\"message\":\"authentication required\"}]}",
            HttpStatusCode.Unauthorized);
    }
}
