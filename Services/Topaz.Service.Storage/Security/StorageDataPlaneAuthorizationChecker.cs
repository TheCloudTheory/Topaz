using Topaz.EventPipeline;
using Topaz.Identity;
using Topaz.Service.Authorization;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Security;

/// <summary>
/// Validates Bearer tokens for Azure Storage data-plane requests, performing a full RBAC check
/// via <see cref="AzureAuthorizationAdapter.PrincipalHasPermissions"/> (same as Key Vault RBAC mode).
/// </summary>
internal sealed class StorageDataPlaneAuthorizationChecker(Pipeline eventPipeline, ITopazLogger logger)
{
    private readonly AzureAuthorizationAdapter _authAdapter = new(eventPipeline, logger);

    /// <summary>
    /// WWW-Authenticate challenge returned when no valid Authorization header is present.
    /// The Azure Storage SDK uses this to discover the token endpoint and retry with a token.
    /// </summary>
    public static string WwwAuthenticateChallenge =>
        $"Bearer authorization=\"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/{GlobalSettings.DefaultTenantId}\"," +
        $" resource=\"https://storage.azure.com\"";

    /// <summary>
    /// Returns true when the caller's JWT identifies a principal that holds one of
    /// <paramref name="requiredPermissions"/> in the given subscription.
    /// </summary>
    public bool IsAuthorizedForBearer(
        SubscriptionIdentifier subscriptionIdentifier,
        string[] requiredPermissions,
        string authHeader)
    {
        var token = JwtHelper.ValidateJwt(authHeader);
        if (token == null)
        {
            logger.LogError("Authentication failure for Storage Bearer scheme. JWT validation failed.");
            return false;
        }

        // Global admin always passes.
        if (token.Subject == Globals.GlobalAdminId) return true;

        return _authAdapter.PrincipalHasPermissions(subscriptionIdentifier, token.Subject, requiredPermissions);
    }
}
