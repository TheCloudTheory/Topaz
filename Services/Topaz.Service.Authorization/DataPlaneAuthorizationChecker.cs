using Topaz.EventPipeline;
using Topaz.Identity;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Authorization;

public abstract class DataPlaneAuthorizationChecker(Pipeline eventPipeline, ITopazLogger logger)
{
    private readonly AzureAuthorizationAdapter _authAdapter = new(eventPipeline, logger);
    
    /// <summary>
    /// Returns true when the caller's JWT identifies a principal that holds one of
    /// <paramref name="requiredPermissions"/> in the given subscription.
    /// </summary>
    public virtual bool IsAuthorizedForBearer(
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