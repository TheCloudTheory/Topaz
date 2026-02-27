using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Topaz.EventPipeline;
using Topaz.Identity;
using Topaz.Service.Authorization.Domain;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Authorization;

public sealed class AzureAuthorizationAdapter(Pipeline eventPipeline, ITopazLogger logger)
{
    private readonly AuthorizationControlPlane _controlPlane = AuthorizationControlPlane.New(eventPipeline, logger);
    
    public (bool isAuthorized, ClaimsPrincipal? principal) IsAuthorized(string[] requiredPermissions, string token, string scope, bool canBypass = false)
    {
        logger.LogDebug(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized),
            "Attempting to check authorization for {0} token against {1} permissions...", token,
            requiredPermissions.Length);
        
        if (requiredPermissions.Length == 0)
        {
            logger.LogDebug(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized),
                "No permissions defined for the endpoint. It's treated as a public endpoint.");
            return (true, null);
        }

        if (canBypass)
        {
            logger.LogDebug(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized), "Bypassing authorization.");
            return (true, null);
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogDebug(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized), "No token provided.");
            return (false, null);
        }

        if (token.StartsWith("SharedKey") || token.StartsWith("SharedAccessSignature"))
        {
            logger.LogDebug(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized),
                "Shared key token - skipping authorization as it's offloaded to the service.");
            return (true, null);;
        }
        
        var validatedToken = JwtHelper.ValidateJwt(token);
        if (validatedToken == null)
        {
            logger.LogError(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized), "Invalid token.");
            return (false, null);;
        }

        logger.LogDebug(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized),
            "Token validated - attempting authorization for `{0}`", validatedToken.Subject);

        if (validatedToken.Audiences.Any(a => a.EndsWith("/.graph")))
        {
            logger.LogDebug(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized),
                "Token is for Microsoft Graph - skipping authorization. This behaviour may change in the future.");
            return (true, GetClaimsPrincipal(validatedToken));
        }

        if (validatedToken.Subject == Globals.GlobalAdminId)
        {
            logger.LogDebug(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized),
                "Token is for global admin - skipping authorization.");
            return (true, GetClaimsPrincipal(validatedToken)); 
        }

        if (!scope.Contains("/subscriptions/"))
        {
            logger.LogDebug(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized),
                "Scope does not contain `/subscriptions/` - skipping authorization.");
            return (true, GetClaimsPrincipal(validatedToken));
        }
        
        var subscriptionIdentifier = SubscriptionIdentifier.From(scope.ExtractValueFromPath(2));
        var assignments =
            _controlPlane.ListSubscriptionRoleAssignmentsByEntraObject(subscriptionIdentifier, validatedToken.Subject);

        if (assignments.Resource == null || assignments.Resource.Length == 0)
        {
            logger.LogDebug(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized),
                "No role assignments found for the given subscription and object ID.");
            return (false, null);
        }

        foreach (var assignment in assignments.Resource)
        {
            var definition = _controlPlane.Get(subscriptionIdentifier,
                RoleDefinitionIdentifier.From(assignment.Properties.RoleDefinitionId));
            
            if (definition.Resource == null)
            {
                logger.LogError(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized),
                    "Failed to retrieve role definition for assignment: `{0}`, reason: `{1}`", assignment.Id, definition.Reason);
                continue;
            }

            if (!PermissionChecks.HasAnyRequiredPermission(definition.Resource.Properties.Permissions,
                    requiredPermissions)) continue;
            
            logger.LogDebug(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized),
                "Found required permissions in role definition: {0}", definition.Resource.Id);
            return (true, GetClaimsPrincipal(validatedToken));
        }
        
        logger.LogDebug(nameof(AzureAuthorizationAdapter), nameof(IsAuthorized),
            "No required permissions found in any role definition for the given subscription and object ID.");
        return (false, null);
    }

    private static ClaimsPrincipal GetClaimsPrincipal(JwtSecurityToken validatedToken)
    {
        return new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, validatedToken.Subject)
        ]));
    }
}