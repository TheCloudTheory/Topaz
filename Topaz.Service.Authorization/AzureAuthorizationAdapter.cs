using Topaz.EventPipeline;
using Topaz.Identity;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Authorization;

public sealed class AzureAuthorizationAdapter(Pipeline eventPipeline, ITopazLogger logger)
{
    private readonly AuthorizationControlPlane _controlPlane = AuthorizationControlPlane.New(eventPipeline, logger);
    
    public bool IsAuthorized(string[] requiredPermissions, string token, string scope)
    {
        logger.LogDebug(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized),
            "Attempting to check authorization for {0} token against {1} permissions...", token,
            requiredPermissions.Length);
        
        var validatedToken = JwtHelper.ValidateJwt(token);
        if (validatedToken == null)
        {
            logger.LogError(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized), "Invalid token.");
            return false;
        }

        logger.LogDebug(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized),
            "Token validated - attempting authorization for `{0}`", validatedToken.Subject);

        var subscriptionIdentifier = SubscriptionIdentifier.From(scope.ExtractValueFromPath(2));
        var assignments =
            _controlPlane.ListSubscriptionRoleAssignmentsByEntraObject(subscriptionIdentifier, validatedToken.Subject);

        if (assignments.Resource == null || assignments.Resource.Length == 0)
        {
            logger.LogDebug(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized),
                "No role assignments found for the given subscription and object ID.");
            return false;
        }

        if (requiredPermissions.Length == 0)
        {
            logger.LogDebug(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized),
                "No permissions defined for the endpoint.");
            return false;
        }
        
        return true;
    }
    
}