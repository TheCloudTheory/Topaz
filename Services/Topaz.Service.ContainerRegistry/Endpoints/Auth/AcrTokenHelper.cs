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
/// Shared authentication helper for the /oauth2/token GET and POST endpoints.
/// Resolves a caller's object ID from a refresh token or Basic Authorization header.
/// Returns <c>null</c> when the caller cannot be authenticated.
/// </summary>
internal static class AcrTokenHelper
{
    /// <summary>
    /// The well-known ACR username sentinel used by Docker and Azure CLI when
    /// authenticating with a token (refresh or access) instead of real credentials.
    /// Defined by the ACR OAuth2 token exchange protocol; independent of Topaz internals.
    /// </summary>
    private const string AcrTokenSentinelUsername = "00000000-0000-0000-0000-000000000000";
    /// <summary>
    /// Resolves the caller's object ID.
    /// <list type="bullet">
    ///   <item><term>refresh_token present</term><description>Validated as a Topaz JWT; subject returned on success, <c>null</c> on failure.</description></item>
    ///   <item><term>Basic Authorization header present</term><description>Credentials checked against the registry's admin account; <c>null</c> if the registry is not found, admin is disabled, or credentials are wrong.</description></item>
    ///   <item><term>No credentials</term><description>Returns <c>null</c> — anonymous requests are rejected.</description></item>
    /// </list>
    /// </summary>
    public static string? ResolveObjectId(
        string? refreshToken,
        HttpContext context,
        ContainerRegistryControlPlane controlPlane,
        ITopazLogger logger)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                var validated = JwtHelper.ValidateJwt(refreshToken);
                return validated?.Subject ?? Globals.GlobalAdminId;
            }
            catch
            {
                logger.LogDebug(nameof(AcrTokenHelper), nameof(ResolveObjectId),
                    "refresh_token validation failed — issuing 401.");
                return null;
            }
        }

        var authorization = context.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrWhiteSpace(authorization) &&
            authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return ValidateBasicAuth(authorization, context, controlPlane, logger);
        }

        logger.LogDebug(nameof(AcrTokenHelper), nameof(ResolveObjectId),
            "No credentials supplied — issuing 401.");
        return null;
    }

    private static string? ValidateBasicAuth(
        string authorization,
        HttpContext context,
        ContainerRegistryControlPlane controlPlane,
        ITopazLogger logger)
    {
        var encoded = authorization["Basic ".Length..].Trim();
        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }
        catch
        {
            logger.LogDebug(nameof(AcrTokenHelper), nameof(ValidateBasicAuth),
                "Basic auth — malformed base64, issuing 401.");
            return null;
        }

        var colon = decoded.IndexOf(':');
        var username = colon > 0 ? decoded[..colon] : decoded;
        var password = colon > 0 ? decoded[(colon + 1)..] : string.Empty;

        // The ACR sentinel username signals that the password is an ACR refresh token
        // (a JWT issued by /oauth2/exchange or by a previous /oauth2/token call).
        // This is how Docker passes token-based credentials to the token endpoint.
        if (string.Equals(username, AcrTokenSentinelUsername, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var validated = JwtHelper.ValidateJwt(password);
                return validated?.Subject ?? Globals.GlobalAdminId;
            }
            catch
            {
                logger.LogDebug(nameof(AcrTokenHelper), nameof(ValidateBasicAuth),
                    "Basic auth — refresh token in password field failed validation, issuing 401.");
                return null;
            }
        }

        var registry = ResolveRegistry(context, controlPlane);
        if (registry == null || !registry.Properties.AdminUserEnabled)
        {
            logger.LogDebug(nameof(AcrTokenHelper), nameof(ValidateBasicAuth),
                "Basic auth — registry not found or admin user disabled.");
            return null;
        }

        if (!string.Equals(registry.Properties.AdminUsername, username, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(registry.Properties.AdminPassword, password, StringComparison.Ordinal))
        {
            logger.LogDebug(nameof(AcrTokenHelper), nameof(ValidateBasicAuth),
                "Basic auth — invalid credentials for user '{0}'.", username);
            return null;
        }

        return Globals.GlobalAdminId;
    }

    private static ContainerRegistryResource? ResolveRegistry(
        HttpContext context,
        ContainerRegistryControlPlane controlPlane)
    {
        var registryName = context.Request.Host.Host.Split('.')[0];
        var identifiers = GlobalDnsEntries.GetEntry(ContainerRegistryService.UniqueName, registryName);
        if (identifiers == null) return null;

        var operation = controlPlane.Get(
            SubscriptionIdentifier.From(identifiers.Value.subscription),
            ResourceGroupIdentifier.From(identifiers.Value.resourceGroup!),
            registryName);
        return operation.Resource;
    }
}
