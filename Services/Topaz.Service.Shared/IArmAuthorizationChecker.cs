using System.Security.Claims;

namespace Topaz.Service.Shared;

/// <summary>
/// Abstraction over the ARM RBAC authorization check used by the Router.
/// Defined in Topaz.Service.Shared so that <see cref="IEndpointDefinition.Authorize"/>
/// can call it directly without a circular dependency.
/// </summary>
public interface IArmAuthorizationChecker
{
    (bool isAuthorized, ClaimsPrincipal? principal) IsAuthorized(
        string[] requiredPermissions, string token, string? scope);
}
