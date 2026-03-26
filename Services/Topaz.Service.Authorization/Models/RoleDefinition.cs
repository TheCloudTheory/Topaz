using Topaz.Service.Shared.Domain;

namespace Topaz.Service.Authorization.Models;

internal sealed class RoleDefinition
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Type { get; init; }

    public string[]? AssignableScopes { get; init; }
    public string? Description { get; init; }
    public Permission[]? Permissions { get; init; }
    public string? RoleName { get; init; }
    public string? RoleType { get; init; }

    public sealed class Permission
    {
        public string[]? Actions { get; init; }
        public string[]? NotActions { get; init; }
        public string[]? DataActions { get; init; }
        public string[]? NotDataActions { get; init; }
        public string? ConditionVersion { get; init; }
        public string? Condition { get; init; }
    }

    public RoleDefinitionResource ToRoleDefinitionResource(SubscriptionIdentifier subscriptionIdentifier)
    {
        var properties = new RoleDefinitionResourceProperties
        {
            RoleName = RoleName,
            Description = Description,
            Type = RoleType,
            AssignableScopes = AssignableScopes,
            Permissions = Permissions?.Select(p => new Permission
            {
                Actions = p.Actions,
                NotActions = p.NotActions,
                DataActions = p.DataActions,
                NotDataActions = p.NotDataActions
            }).ToArray()
        };

        return new RoleDefinitionResource(subscriptionIdentifier, Name!, properties);
    }
}
