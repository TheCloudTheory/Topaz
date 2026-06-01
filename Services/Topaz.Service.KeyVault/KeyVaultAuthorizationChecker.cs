using Topaz.EventPipeline;
using Topaz.Identity;
using Topaz.Service.Authorization;
using Topaz.Service.KeyVault.Models;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

/// <summary>
/// Checks whether a caller is allowed to perform a Key Vault data-plane operation,
/// using either Access Policies or RBAC depending on the vault configuration.
/// </summary>
internal sealed class KeyVaultAuthorizationChecker(Pipeline eventPipeline, ITopazLogger logger)
{
    private readonly AzureAuthorizationAdapter _authAdapter = new(eventPipeline, logger);

    /// <summary>
    /// Builds the WWW-Authenticate challenge header value for a request that arrived without a Bearer token.
    /// The Azure SDK uses this to discover the token endpoint and then retries with a token.
    /// The authorization URL points to Topaz's ARM token endpoint (port 8899) so that
    /// both MSAL v2 and old go-autorest v1 clients can acquire a token and retry.
    /// The resource is set to the BASE vault domain (vault name prefix stripped), matching
    /// real Azure's pattern where resource="https://vault.azure.net" rather than
    /// "https://myvault.vault.azure.net". The Python SDK verifies via
    ///   request_domain.endswith("." + resource_domain)
    /// so the resource must be a parent domain of the request host:
    ///   request_domain  = "myvault.vault.topaz.local.dev:8898"
    ///   resource_domain = "vault.topaz.local.dev:8898"   ← first label stripped
    ///   check: "myvault.vault.topaz.local.dev:8898".endswith(".vault.topaz.local.dev:8898") → true
    /// </summary>
    /// <param name="host">The Host header value from the incoming request (e.g. "myvault.vault.topaz.local.dev:8898").</param>
    public static string BuildWwwAuthenticateChallenge(string host)
    {
        // Strip the vault-name label: "myvault.vault.topaz.local.dev:8898" → "vault.topaz.local.dev:8898"
        var firstDot = host.IndexOf('.');
        var baseDomain = firstDot >= 0 ? host[(firstDot + 1)..] : host;
        return $"Bearer authorization=\"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/{GlobalSettings.DefaultTenantId}\"," +
               $" resource=\"https://{baseDomain}\"";
    }

    /// <summary>
    /// Returns true when the caller identified by <paramref name="authHeader"/> is allowed to perform
    /// the operation described by <paramref name="requiredArmPermissions"/> on the given vault.
    /// </summary>
    /// <param name="authHeader">The raw Authorization header value (e.g. "Bearer eyJ...").</param>
    /// <param name="vault">The target vault resource.</param>
    /// <param name="requiredArmPermissions">ARM-style permissions required for RBAC mode (from the endpoint).</param>
    /// <param name="accessPolicyPermission">
    /// KV access-policy permission name required in access-policy mode, e.g. "set", "get", "create".
    /// Pass null only when no specific permission applies.
    /// </param>
    /// <param name="accessPolicyScope">Which permission bucket to check: "secrets", "keys", or "certificates".</param>
    public bool IsAuthorized(
        string? authHeader,
        KeyVaultResource vault,
        string[] requiredArmPermissions,
        string? accessPolicyPermission,
        string accessPolicyScope = "secrets")
    {
        // No token → deny; once authorization is checked, a valid token is required.
        if (string.IsNullOrEmpty(authHeader)) return false;

        var token = JwtHelper.ValidateJwt(authHeader);
        if (token == null)
        {
            logger.LogDebug(nameof(KeyVaultAuthorizationChecker), nameof(IsAuthorized),
                "Invalid or unrecognized JWT — denying access.");
            return false;
        }

        // Global admin always passes.
        if (token.Subject == Globals.GlobalAdminId) return true;

        if (vault.Properties.EnableRbacAuthorization)
        {
            var subscriptionId = vault.GetSubscription();
            logger.LogDebug(nameof(KeyVaultAuthorizationChecker), nameof(IsAuthorized),
                "Vault uses RBAC — checking role assignments for principal {0} in subscription {1}.",
                token.Subject, subscriptionId);
            return _authAdapter.PrincipalHasPermissions(subscriptionId, token.Subject, requiredArmPermissions);
        }

        // Access-policy mode.
        logger.LogDebug(nameof(KeyVaultAuthorizationChecker), nameof(IsAuthorized),
            "Vault uses access policies — checking policy for principal {0}.", token.Subject);

        var policy = vault.Properties.AccessPolicies.FirstOrDefault(p =>
            string.Equals(p.ObjectId, token.Subject, StringComparison.OrdinalIgnoreCase));

        if (policy == null)
        {
            logger.LogDebug(nameof(KeyVaultAuthorizationChecker), nameof(IsAuthorized),
                "No access policy found for principal {0}.", token.Subject);
            return false;
        }

        if (accessPolicyPermission == null) return true;

        var grantedPermissions = accessPolicyScope.ToLowerInvariant() switch
        {
            "keys" => policy.Permissions?.Keys,
            "certificates" => policy.Permissions?.Certificates,
            _ => policy.Permissions?.Secrets,
        };

        return grantedPermissions?.Contains(accessPolicyPermission, StringComparer.OrdinalIgnoreCase) == true;
    }
}
