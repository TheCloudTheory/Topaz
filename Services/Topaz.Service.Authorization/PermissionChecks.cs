
using Topaz.Service.Authorization.Models;

namespace Topaz.Service.Authorization;

internal static class PermissionChecks
{
    public static bool HasAnyRequiredPermission(
        IEnumerable<RoleDefinition.Permission>? grantedPermissions,
        IEnumerable<string> requiredPermissions)
    {
        var required = requiredPermissions?.ToArray() ?? Array.Empty<string>();

        return (from block in grantedPermissions ?? []
            let actions = block.Actions ?? Enumerable.Empty<string>()
            let notActions = (block.NotActions ?? Enumerable.Empty<string>()).ToArray()
            where (from req in required
                let allowedByActions = actions.Any<string>(a => Matches(a, req))
                where allowedByActions
                select notActions.Any(na => Matches(na, req))).Any(deniedByNotActions => !deniedByNotActions)
            select actions).Any();
    }

    private static bool Matches(string grantedPattern, string requiredAction)
    {
        // same Matches as in option #2
        if (string.IsNullOrWhiteSpace(grantedPattern) || string.IsNullOrWhiteSpace(requiredAction))
            return false;

        if (grantedPattern.Equals(requiredAction, StringComparison.OrdinalIgnoreCase))
            return true;

        if (grantedPattern == "*")
            return true;

        if (grantedPattern.StartsWith("*/", StringComparison.Ordinal) &&
            requiredAction.EndsWith(grantedPattern[1..], StringComparison.OrdinalIgnoreCase))
            return true;

        const string star = "/*";
        var starIndex = grantedPattern.IndexOf(star, StringComparison.Ordinal);
        if (starIndex >= 0)
        {
            var prefix = grantedPattern[..starIndex];
            var suffix = grantedPattern[(starIndex + star.Length)..];
            return requiredAction.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                   requiredAction.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}